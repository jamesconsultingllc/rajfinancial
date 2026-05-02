using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace RajFinancial.Api.Services.Ai.Telemetry;

/// <summary>
/// <see cref="AIFunction"/> decorator that emits per-invocation activity + counters +
/// histograms on the central <see cref="AiToolTelemetry"/> instruments and applies
/// <see cref="IAiTelemetryRedactor"/> to argument values before recording them as
/// activity event tags.
/// </summary>
/// <remarks>
/// <para>
/// The decorator is independent of the chat-client middleware order. The
/// <c>UseFunctionInvocation</c> middleware will eventually call
/// <see cref="InvokeAsync"/> on this instance, at which point we own the timing,
/// outcome, and redacted-preview emission — regardless of whether the surrounding
/// chat-client pipeline wraps OTel inside or outside the function-invocation loop.
/// </para>
/// <para>
/// All non-invocation members (Name, Description, JsonSchema, etc.) forward to the inner
/// instance — the model and tool-call middleware see exactly the same metadata as if
/// the decorator weren't there.
/// </para>
/// </remarks>
internal sealed class TelemetryAIFunction : AIFunction
{
    private readonly AIFunction _inner;
    private readonly IAiTelemetryRedactor _redactor;
    private readonly string _scope;

    internal TelemetryAIFunction(AIFunction inner, IAiTelemetryRedactor redactor, string scope)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(redactor);
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        _inner = inner;
        _redactor = redactor;
        _scope = scope;
    }

    /// <summary>The wrapped tool. Exposed for tests + diagnostics; not part of the public surface.</summary>
    internal AIFunction Inner => _inner;

    /// <summary>Domain scope this tool was registered under.</summary>
    internal string Scope => _scope;

    public override string Name => _inner.Name;

    public override string Description => _inner.Description;

    public override JsonElement JsonSchema => _inner.JsonSchema;

    public override JsonElement? ReturnJsonSchema => _inner.ReturnJsonSchema;

    public override JsonSerializerOptions JsonSerializerOptions => _inner.JsonSerializerOptions;

    public override MethodInfo? UnderlyingMethod => _inner.UnderlyingMethod;

    public override IReadOnlyDictionary<string, object?> AdditionalProperties => _inner.AdditionalProperties;

    protected override async ValueTask<object?> InvokeCoreAsync(
        AIFunctionArguments arguments,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        using var activity = AiToolTelemetry.ActivitySource.StartActivity(
            AiToolTelemetry.SpanName,
            ActivityKind.Internal);

        if (activity is not null)
        {
            activity.SetTag(AiToolTelemetry.TagToolName, _inner.Name);
            activity.SetTag(AiToolTelemetry.TagToolScope, _scope);
            EmitRedactedArgumentEvent(activity, arguments);
        }

        var stopwatch = Stopwatch.StartNew();
        string outcome = AiToolTelemetry.OutcomeError;
        string? errorType = null;

#pragma warning disable S1854 // Sonar's flow analysis misses use-in-finally for outcome/errorType
        try
        {
            var result = await _inner.InvokeAsync(arguments, cancellationToken).ConfigureAwait(false);
            outcome = AiToolTelemetry.OutcomeSuccess;
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            outcome = AiToolTelemetry.OutcomeCancelled;
            errorType = nameof(OperationCanceledException);
            throw;
        }
        catch (Exception ex)
        {
            errorType = ex.GetType().Name;
            // Don't pass ex.Message anywhere telemetry might export it: tool exceptions
            // commonly include argument values (e.g., "merchant 'walmart' not allowed")
            // which would bypass our argument redaction. Activity.AddException(ex) writes
            // the raw message to the standard `exception.message` tag, so we deliberately
            // do NOT call it. Instead we emit a sanitized custom event with only the
            // exception type and tool metadata.
            activity?.SetStatus(ActivityStatusCode.Error, ex.GetType().Name);
            activity?.AddEvent(new ActivityEvent(
                AiToolTelemetry.EventToolException,
                tags: new ActivityTagsCollection
                {
                    [AiToolTelemetry.TagErrorType] = ex.GetType().FullName,
                    [AiToolTelemetry.TagToolName] = _inner.Name,
                    [AiToolTelemetry.TagToolScope] = _scope,
                }));
            throw;
        }
#pragma warning restore S1854
        finally
        {
            stopwatch.Stop();

            // TagList is a struct — avoids per-call KeyValuePair[] allocation on the hot path.
            var tags = new TagList
            {
                { AiToolTelemetry.TagToolName, _inner.Name },
                { AiToolTelemetry.TagToolScope, _scope },
                { AiToolTelemetry.TagToolOutcome, outcome },
            };

            AiToolTelemetry.ToolInvocations.Add(1, tags);
            AiToolTelemetry.ToolDurationMs.Record(stopwatch.Elapsed.TotalMilliseconds, tags);

            if (activity is not null)
            {
                activity.SetTag(AiToolTelemetry.TagToolOutcome, outcome);
                if (errorType is not null)
                {
                    activity.SetTag(AiToolTelemetry.TagErrorType, errorType);
                }
            }
        }
    }

    private void EmitRedactedArgumentEvent(Activity activity, AIFunctionArguments arguments)
    {
        var tags = new ActivityTagsCollection();
        foreach (var kvp in arguments)
        {
            var safe = _redactor.Redact(_inner.Name, kvp.Key, kvp.Value);
            tags[kvp.Key] = safe;
        }

        activity.AddEvent(new ActivityEvent(AiToolTelemetry.EventArgsRedacted, tags: tags));
    }
}

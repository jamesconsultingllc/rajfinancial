using System.Diagnostics;
using System.Diagnostics.Metrics;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using RajFinancial.Api.Services.Ai.Telemetry;
using RajFinancial.Api.Services.Ai.Tools;

namespace RajFinancial.Api.Tests.Services.Ai.Telemetry;

[Collection("AiToolTelemetry")]
public class TelemetryAIFunctionTests
{
    private static IAiTelemetryRedactor Redactor() => new DefaultAiTelemetryRedactor(
        Options.Create(new AiTelemetryRedactorOptions
        {
            MerchantHashSecret = new string('z', 32),
            HmacPrefixLength = 12,
        }));

    private sealed class CapturingActivityListener : IDisposable
    {
        private readonly ActivityListener _listener;
        public List<Activity> Activities { get; } = [];

        public CapturingActivityListener()
        {
            _listener = new ActivityListener
            {
                ShouldListenTo = src => src.Name == "RajFinancial.Api.Ai",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStarted = a => Activities.Add(a),
            };
            ActivitySource.AddActivityListener(_listener);
        }

        public void Dispose() => _listener.Dispose();
    }

    private sealed class CapturingMeterListener : IDisposable
    {
        private readonly MeterListener _listener;
        public List<(string Name, long Value, KeyValuePair<string, object?>[] Tags)> Counters { get; } = [];
        public List<(string Name, double Value, KeyValuePair<string, object?>[] Tags)> Histograms { get; } = [];

        public CapturingMeterListener()
        {
            _listener = new MeterListener
            {
                InstrumentPublished = (instrument, l) =>
                {
                    if (instrument.Meter.Name == "RajFinancial.Api.Ai")
                    {
                        l.EnableMeasurementEvents(instrument);
                    }
                },
            };
            _listener.SetMeasurementEventCallback<long>((inst, val, tags, _) =>
                Counters.Add((inst.Name, val, tags.ToArray())));
            _listener.SetMeasurementEventCallback<double>((inst, val, tags, _) =>
                Histograms.Add((inst.Name, val, tags.ToArray())));
            _listener.Start();
        }

        public void Dispose() => _listener.Dispose();
    }

    [Fact]
    public async Task Success_emits_activity_counter_and_histogram_with_success_tag()
    {
        using var activities = new CapturingActivityListener();
        using var meters = new CapturingMeterListener();

        var inner = AIFunctionFactory.Create((int x) => x + 1, name: "diag.add");
        var subject = new TelemetryAIFunction(inner, Redactor(), AiToolScopes.Diagnostics);

        var args = new AIFunctionArguments { ["x"] = 41 };
        var result = await subject.InvokeAsync(args);

        result.Should().NotBeNull();

        var act = activities.Activities.Single();
        act.OperationName.Should().Be("Ai.ToolCall");
        act.GetTagItem("tool.name").Should().Be("diag.add");
        act.GetTagItem("tool.scope").Should().Be(AiToolScopes.Diagnostics);
        act.GetTagItem("tool.outcome").Should().Be("success");

        meters.Counters.Should().Contain(c =>
            c.Name == "ai.tool.invocations.count" && c.Value == 1
            && c.Tags.Any(t => t.Key == "tool.outcome" && (string?)t.Value == "success"));
        meters.Histograms.Should().Contain(h => h.Name == "ai.tool.duration.ms");
    }

    [Fact]
    public async Task Exception_propagates_and_emits_error_outcome_with_status()
    {
        using var activities = new CapturingActivityListener();
        using var meters = new CapturingMeterListener();

        var inner = AIFunctionFactory.Create(new Func<int, int>(x => throw new InvalidOperationException("boom")), name: "diag.fail");
        var subject = new TelemetryAIFunction(inner, Redactor(), AiToolScopes.Diagnostics);

        var args = new AIFunctionArguments { ["x"] = 1 };
        var act = async () => await subject.InvokeAsync(args);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*boom*");

        var activity = activities.Activities.Single();
        activity.GetTagItem("tool.outcome").Should().Be("error");
        activity.GetTagItem("error.type").Should().Be(nameof(InvalidOperationException));
        activity.Status.Should().Be(ActivityStatusCode.Error);

        // PII guard: the raw exception message must not leak via the standard OTel
        // exception event tags (exception.message, exception.stacktrace, exception.type).
        // Our sanitized "tool.exception" event carries only the type + tool metadata.
        var exceptionEvent = activity.Events.SingleOrDefault(e => e.Name == "tool.exception");
        exceptionEvent.Name.Should().Be("tool.exception");
        var exceptionTags = exceptionEvent.Tags.ToDictionary(t => t.Key, t => t.Value);
        exceptionTags.Should().ContainKey("error.type");
        exceptionTags["error.type"].Should().Be(typeof(InvalidOperationException).FullName);
        exceptionTags.Should().NotContainKey("exception.message");
        exceptionTags.Should().NotContainKey("exception.stacktrace");
        activity.Events.Should().NotContain(e => e.Name == "exception",
            because: "Activity.AddException(ex) emits an 'exception' event whose " +
                     "exception.message tag would bypass the AI redactor.");

        meters.Counters.Should().Contain(c =>
            c.Tags.Any(t => t.Key == "tool.outcome" && (string?)t.Value == "error"));
    }

    [Fact]
    public async Task Cancellation_emits_cancelled_outcome_and_propagates()
    {
        using var activities = new CapturingActivityListener();

        var inner = AIFunctionFactory.Create(
            new Func<CancellationToken, Task<int>>(async ct =>
            {
                await Task.Delay(Timeout.Infinite, ct);
                return 0;
            }),
            name: "diag.wait");
        var subject = new TelemetryAIFunction(inner, Redactor(), AiToolScopes.Diagnostics);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await subject.InvokeAsync(new AIFunctionArguments(), cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();

        var activity = activities.Activities.Single();
        activity.GetTagItem("tool.outcome").Should().Be("cancelled");
        activity.GetTagItem("error.type").Should().Be(nameof(OperationCanceledException));
    }

    [Fact]
    public async Task Redacted_arguments_event_emitted_with_redacted_values()
    {
        using var activities = new CapturingActivityListener();

        var inner = AIFunctionFactory.Create((string merchantName, int pageNumber) => "ok", name: "txn.find");
        var subject = new TelemetryAIFunction(inner, Redactor(), AiToolScopes.Diagnostics);

        var args = new AIFunctionArguments
        {
            ["merchantName"] = "Starbucks",
            ["pageNumber"] = 1,
        };
        await subject.InvokeAsync(args);

        var ev = activities.Activities.Single().Events.Single(e => e.Name == "tool.arguments.redacted");
        var tags = ev.Tags.ToDictionary(t => t.Key, t => t.Value);
        tags["merchantName"].Should().BeOfType<string>().Which.Should().StartWith("[REDACTED:HMAC=");
        tags["pageNumber"].Should().Be("1");
    }

    [Fact]
    public void Forwards_metadata_to_inner_unchanged()
    {
        var inner = AIFunctionFactory.Create((int x) => x, name: "diag.identity", description: "d");
        var subject = new TelemetryAIFunction(inner, Redactor(), AiToolScopes.Diagnostics);

        subject.Name.Should().Be(inner.Name);
        subject.Description.Should().Be(inner.Description);
        subject.JsonSchema.GetRawText().Should().Be(inner.JsonSchema.GetRawText());
        subject.Inner.Should().BeSameAs(inner);
        subject.Scope.Should().Be(AiToolScopes.Diagnostics);
    }
}

using Microsoft.Extensions.Options;
using RajFinancial.Shared.Contracts.Ai;

namespace RajFinancial.Api.Services.Ai;

/// <summary>
/// Shape-validation for <see cref="AiOptions"/>. Wired into
/// <c>services.AddOptions&lt;AiOptions&gt;().ValidateOnStart()</c> so misconfiguration fails
/// host startup rather than first AI call.
/// </summary>
/// <remarks>
/// <para>
/// This validator deliberately checks <b>shape only</b>:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="AiOptions.Providers"/> non-empty.</description></item>
///   <item><description><see cref="AiOptions.DefaultProvider"/> is a key in
///   <see cref="AiOptions.Providers"/>.</description></item>
///   <item><description>Each configured provider has a non-empty
///   <see cref="AiProviderOptions.Model"/> and <see cref="AiProviderOptions.ApiKeyEnvVar"/>.</description></item>
/// </list>
/// <para>
/// Provider-implementation availability (i.e., is an <c>IChatClientProvider</c> registered
/// for the configured id?) is intentionally <b>not</b> checked here — that's the factory's
/// responsibility at <c>GetClient</c> time. Splitting the concerns lets a partial deploy
/// (config-only or impl-only) fail with a precise, actionable error at the right layer.
/// </para>
/// <para>
/// <b>Security (OWASP A05):</b> Misconfiguration is a security failure. Failing fast at
/// startup prevents a deploy from running with an unvalidated AI surface.
/// </para>
/// </remarks>
internal sealed class AiOptionsValidator : IValidateOptions<AiOptions>
{
    public ValidateOptionsResult Validate(string? name, AiOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (options.Providers.Count == 0)
        {
            failures.Add($"{AiOptions.SectionName}:Providers must contain at least one provider entry.");
        }
        else if (!options.Providers.ContainsKey(options.DefaultProvider))
        {
            failures.Add(
                $"{AiOptions.SectionName}:DefaultProvider is '{options.DefaultProvider}' but no entry " +
                $"with that key exists in {AiOptions.SectionName}:Providers.");
        }

        foreach (var (id, provider) in options.Providers)
        {
            if (provider is null)
            {
                failures.Add($"{AiOptions.SectionName}:Providers:{id} is null.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(provider.Model))
            {
                failures.Add($"{AiOptions.SectionName}:Providers:{id}:Model is required.");
            }

            if (string.IsNullOrWhiteSpace(provider.ApiKeyEnvVar))
            {
                failures.Add($"{AiOptions.SectionName}:Providers:{id}:ApiKeyEnvVar is required.");
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}

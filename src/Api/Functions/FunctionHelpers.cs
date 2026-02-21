// ============================================================================
// RAJ Financial — Function Helpers
// ============================================================================
// Shared utilities for Azure Functions endpoints: common JSON serialization
// options and structured error response writing.
// ============================================================================

using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Azure.Functions.Worker.Http;
using RajFinancial.Api.Middleware;

namespace RajFinancial.Api.Functions;

/// <summary>
/// Shared helper utilities for Azure Functions HTTP endpoints.
/// </summary>
/// <remarks>
///     Centralises <see cref="JsonOptions"/> and <see cref="WriteErrorResponse"/>
///     so they are defined once rather than duplicated in every function class.
/// </remarks>
internal static class FunctionHelpers
{
    /// <summary>
    /// Shared JSON serializer options: camelCase naming, null-value omission.
    /// </summary>
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Writes a structured error response with the standard
    /// <see cref="ApiErrorResponse"/> format.
    /// </summary>
    /// <param name="req">The incoming HTTP request.</param>
    /// <param name="statusCode">The HTTP status code to return.</param>
    /// <param name="code">Machine-readable error code (e.g. <c>AUTH_REQUIRED</c>).</param>
    /// <param name="message">Human-readable error message.</param>
    /// <returns>The fully-written <see cref="HttpResponseData"/>.</returns>
    internal static async Task<HttpResponseData> WriteErrorResponse(
        HttpRequestData req,
        HttpStatusCode statusCode,
        string code,
        string message)
    {
        var response = req.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await response.WriteStringAsync(
            JsonSerializer.Serialize(new ApiErrorResponse
            {
                Code = code,
                Message = message
            }, JsonOptions));

        return response;
    }
}

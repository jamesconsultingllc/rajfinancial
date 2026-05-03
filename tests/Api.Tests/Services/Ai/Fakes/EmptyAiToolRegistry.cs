using Microsoft.Extensions.AI;
using RajFinancial.Api.Services.Ai.Tools;

namespace RajFinancial.Api.Tests.Services.Ai.Fakes;

internal sealed class EmptyAiToolRegistry : IAiToolRegistry
{
    public static readonly EmptyAiToolRegistry Instance = new();

    private EmptyAiToolRegistry() { }

    public bool IsEmpty => true;
    public int Count => 0;
    public IReadOnlyList<AIFunction> GetAll() => [];
    public IReadOnlyList<AIFunction> GetByScope(string scope) => [];
}

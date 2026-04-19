// ============================================================================
// Architecture tests — NetArchTest.Rules
// ----------------------------------------------------------------------------
// These tests encode the "Architecture Conventions (Enforced)" section of
// AGENT.md as executable assertions. They fail the build if a violation is
// introduced, so new code lands under the same guardrails as old code.
//
// Adding a new rule: write an xUnit [Fact] that builds a NetArchTest
// `Types.InAssembly(...)` predicate chain and asserts `.GetResult().IsSuccessful`
// with a diagnostic message listing failing types.
//
// Grandfathered violations: document the exception list in a `_Exceptions`
// constant and filter them out of the predicate — do NOT silently disable
// the test. Every exception must be traceable to a work item.
// ============================================================================

global using FluentAssertions;
global using NetArchTest.Rules;

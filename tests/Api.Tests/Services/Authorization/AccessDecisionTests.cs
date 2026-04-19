using FluentAssertions;
using RajFinancial.Api.Services.Authorization;
using RajFinancial.Shared.Entities;
using RajFinancial.Shared.Entities.Access;

namespace RajFinancial.Api.Tests.Services.Authorization;

/// <summary>
/// Unit tests for <see cref="AccessDecision"/>.
/// Verifies the value type correctly represents access control decisions
/// as part of OWASP A01:2025 (Broken Access Control) compliance.
/// </summary>
public class AccessDecisionTests
{
    // =========================================================================
    // Grant factory method — valid reasons
    // =========================================================================

    [Fact]
    public void Grant_ResourceOwner_ReturnsGrantedDecision()
    {
        var decision = AccessDecision.Grant(AccessDecisionReason.ResourceOwner, AccessType.Owner);

        decision.IsGranted.Should().BeTrue();
        decision.Reason.Should().Be(AccessDecisionReason.ResourceOwner);
        decision.GrantedAccessLevel.Should().Be(AccessType.Owner);
    }

    [Fact]
    public void Grant_DataAccessGrant_ReturnsGrantedDecisionWithAccessLevel()
    {
        var decision = AccessDecision.Grant(AccessDecisionReason.DataAccessGrant, AccessType.Read);

        decision.IsGranted.Should().BeTrue();
        decision.Reason.Should().Be(AccessDecisionReason.DataAccessGrant);
        decision.GrantedAccessLevel.Should().Be(AccessType.Read);
    }

    [Fact]
    public void Grant_Administrator_ReturnsGrantedDecisionWithFullAccess()
    {
        var decision = AccessDecision.Grant(AccessDecisionReason.Administrator, AccessType.Full);

        decision.IsGranted.Should().BeTrue();
        decision.Reason.Should().Be(AccessDecisionReason.Administrator);
        decision.GrantedAccessLevel.Should().Be(AccessType.Full);
    }

    // =========================================================================
    // Grant factory method — invalid reason
    // =========================================================================

    [Fact]
    public void Grant_WithDeniedReason_ThrowsArgumentException()
    {
        var act = () => AccessDecision.Grant(AccessDecisionReason.Denied, AccessType.Read);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("reason");
    }

    // =========================================================================
    // Grant factory method — all access types
    // =========================================================================

    [Theory]
    [InlineData(AccessType.Owner)]
    [InlineData(AccessType.Full)]
    [InlineData(AccessType.Read)]
    [InlineData(AccessType.Limited)]
    public void Grant_AllAccessTypes_SetsGrantedAccessLevel(AccessType accessType)
    {
        var decision = AccessDecision.Grant(AccessDecisionReason.ResourceOwner, accessType);

        decision.GrantedAccessLevel.Should().Be(accessType);
    }

    // =========================================================================
    // Deny factory method
    // =========================================================================

    [Fact]
    public void Deny_ReturnsDeniedDecision()
    {
        var decision = AccessDecision.Deny();

        decision.IsGranted.Should().BeFalse();
        decision.Reason.Should().Be(AccessDecisionReason.Denied);
        decision.GrantedAccessLevel.Should().BeNull();
    }

    // =========================================================================
    // Value equality (record semantics)
    // =========================================================================

    [Fact]
    public void Grant_SameValues_AreEqual()
    {
        var decision1 = AccessDecision.Grant(AccessDecisionReason.ResourceOwner, AccessType.Owner);
        var decision2 = AccessDecision.Grant(AccessDecisionReason.ResourceOwner, AccessType.Owner);

        decision1.Should().Be(decision2);
    }

    [Fact]
    public void Grant_DifferentReasons_AreNotEqual()
    {
        var decision1 = AccessDecision.Grant(AccessDecisionReason.ResourceOwner, AccessType.Full);
        var decision2 = AccessDecision.Grant(AccessDecisionReason.Administrator, AccessType.Full);

        decision1.Should().NotBe(decision2);
    }

    [Fact]
    public void Grant_DifferentAccessLevels_AreNotEqual()
    {
        var decision1 = AccessDecision.Grant(AccessDecisionReason.DataAccessGrant, AccessType.Read);
        var decision2 = AccessDecision.Grant(AccessDecisionReason.DataAccessGrant, AccessType.Full);

        decision1.Should().NotBe(decision2);
    }

    [Fact]
    public void Deny_MultipleInstances_AreEqual()
    {
        var decision1 = AccessDecision.Deny();
        var decision2 = AccessDecision.Deny();

        decision1.Should().Be(decision2);
    }

    [Fact]
    public void Grant_AndDeny_AreNotEqual()
    {
        var granted = AccessDecision.Grant(AccessDecisionReason.ResourceOwner, AccessType.Owner);
        var denied = AccessDecision.Deny();

        granted.Should().NotBe(denied);
    }

    // =========================================================================
    // Immutability
    // =========================================================================

    [Fact]
    public void AccessDecision_IsImmutable_RecordWithInitOnlyProperties()
    {
        var decision = AccessDecision.Grant(AccessDecisionReason.ResourceOwner, AccessType.Owner);

        // Record 'with' expression creates a new instance, not mutating the original
        var modified = decision with { Reason = AccessDecisionReason.Administrator };

        decision.Reason.Should().Be(AccessDecisionReason.ResourceOwner);
        modified.Reason.Should().Be(AccessDecisionReason.Administrator);
        decision.Should().NotBe(modified);
    }
}

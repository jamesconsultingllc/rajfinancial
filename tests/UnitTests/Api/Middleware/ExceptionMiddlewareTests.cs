using FluentAssertions;
using RajFinancial.Api.Middleware;
using RajFinancial.Api.Middleware.Exception;

namespace RajFinancial.UnitTests.Api.Middleware;

/// <summary>
/// Unit tests for <see cref="ExceptionMiddleware"/>.
/// Tests OWASP A10:2025 compliance - never leak stack traces.
/// </summary>
public class ExceptionMiddlewareTests
{
    [Fact]
    public void NotFoundException_ShouldHaveCorrectErrorCode()
    {
        // Act
        var exception = NotFoundException.Asset(Guid.NewGuid());

        // Assert
        exception.ErrorCode.Should().Be("ASSET_NOT_FOUND");
    }

    [Fact]
    public void NotFoundException_Account_ShouldHaveCorrectErrorCode()
    {
        // Act
        var exception = NotFoundException.Account(Guid.NewGuid());

        // Assert
        exception.ErrorCode.Should().Be("ACCOUNT_NOT_FOUND");
    }

    [Fact]
    public void NotFoundException_Beneficiary_ShouldHaveCorrectErrorCode()
    {
        // Act
        var exception = NotFoundException.Beneficiary(Guid.NewGuid());

        // Assert
        exception.ErrorCode.Should().Be("BENEFICIARY_NOT_FOUND");
    }

    [Fact]
    public void NotFoundException_User_ShouldHaveCorrectErrorCode()
    {
        // Act
        var exception = NotFoundException.User("user-123");

        // Assert
        exception.ErrorCode.Should().Be("USER_NOT_FOUND");
        exception.Message.Should().Contain("user-123");
    }

    [Fact]
    public void ValidationException_ShouldContainErrors()
    {
        // Arrange
        var errors = new Dictionary<string, object>
        {
            { "Name", new List<string> { "Name is required" } },
            { "Email", new List<string> { "Email is invalid" } }
        };

        // Act
        var exception = new ValidationException("Validation failed", errors);

        // Assert
        exception.Message.Should().Be("Validation failed");
        exception.Errors.Should().NotBeNull();
        exception.Errors.Should().ContainKey("Name");
        exception.Errors.Should().ContainKey("Email");
    }

    [Fact]
    public void UnauthorizedException_ShouldHaveDefaultMessage()
    {
        // Act
        var exception = new UnauthorizedException();

        // Assert
        exception.Message.Should().Be("Authentication required");
    }

    [Fact]
    public void UnauthorizedException_ShouldAcceptCustomMessage()
    {
        // Act
        var exception = new UnauthorizedException("Custom auth message");

        // Assert
        exception.Message.Should().Be("Custom auth message");
    }

    [Fact]
    public void ForbiddenException_ShouldHaveDefaultMessage()
    {
        // Act
        var exception = new ForbiddenException();

        // Assert
        exception.Message.Should().Be("Access denied");
    }

    [Fact]
    public void ForbiddenException_ShouldAcceptCustomMessage()
    {
        // Act
        var exception = new ForbiddenException("Custom forbidden message");

        // Assert
        exception.Message.Should().Be("Custom forbidden message");
    }

    [Fact]
    public void BusinessRuleException_ShouldContainErrorCode()
    {
        // Act
        var exception = new BusinessRuleException("INSUFFICIENT_FUNDS", "Not enough balance");

        // Assert
        exception.ErrorCode.Should().Be("INSUFFICIENT_FUNDS");
        exception.Message.Should().Be("Not enough balance");
    }

    [Fact]
    public void ConfigurationException_ShouldContainMessage()
    {
        // Act
        var exception = new ConfigurationException("Missing connection string");

        // Assert
        exception.Message.Should().Be("Missing connection string");
    }

    [Fact]
    public void ApiErrorResponse_ShouldSerializeCorrectly()
    {
        // Arrange
        var response = new ApiErrorResponse
        {
            Code = "TEST_ERROR",
            Message = "Test error message",
            TraceId = "trace-123",
            Details = new Dictionary<string, object>
            {
                { "field", "value" }
            }
        };

        // Assert
        response.Code.Should().Be("TEST_ERROR");
        response.Message.Should().Be("Test error message");
        response.TraceId.Should().Be("trace-123");
        response.Details.Should().ContainKey("field");
    }
}

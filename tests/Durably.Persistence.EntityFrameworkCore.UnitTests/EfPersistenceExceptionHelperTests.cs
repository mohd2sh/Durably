using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Durably.Persistence.EntityFrameworkCore.UnitTests;

public sealed class EfPersistenceExceptionHelperTests
{
    [Theory]
    [InlineData("Violation of PRIMARY KEY constraint 'PK_Executions'.")]
    [InlineData("duplicate key value violates unique constraint")]
    [InlineData("UNIQUE constraint failed: durable.Executions")]
    [InlineData("23505: duplicate key value")]
    public void IsDuplicateKey_true_for_known_messages(string message)
    {
        // Arrange
        var exception = new InvalidOperationException(message);

        // Act
        var result = EfPersistenceExceptionHelper.IsDuplicateKey(exception);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsDuplicateKey_true_when_message_is_on_inner_exception()
    {
        // Arrange
        var inner = new Exception("Violation of PRIMARY KEY constraint 'PK_Executions'.");
        var outer = new DbUpdateException("An error occurred while saving the entity changes.", inner);

        // Act
        var result = EfPersistenceExceptionHelper.IsDuplicateKey(outer);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsDuplicateKey_false_for_unrelated_errors()
    {
        // Arrange
        var exception = new InvalidOperationException("connection refused");

        // Act
        var result = EfPersistenceExceptionHelper.IsDuplicateKey(exception);

        // Assert
        Assert.False(result);
    }
}

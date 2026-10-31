using FluentAssertions;
using Mms.Domain.Entities;

namespace Mms.Domain.UnitTests;

public class RefreshTokenTests
{
    [Fact]
    public void RefreshToken_WhenCreated_ShouldBeActive()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddDays(7);

        // Act
        var token = new RefreshToken(Guid.NewGuid(), "sample_token", expiresAt, now);

        // Assert
        token.IsActive(now).Should().BeTrue();
        token.IsRevoked.Should().BeFalse();
        token.IsExpired(now).Should().BeFalse();
    }

    [Fact]
    public void RefreshToken_WhenPastExpiry_ShouldBeExpired()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddDays(7);
        var token = new RefreshToken(Guid.NewGuid(), "sample_token", expiresAt, now);

        // Act
        var checkTime = expiresAt.AddSeconds(1);

        // Assert
        token.IsExpired(checkTime).Should().BeTrue();
        token.IsActive(checkTime).Should().BeFalse();
    }

    [Fact]
    public void RefreshToken_WhenRevoked_ShouldNotBeActive()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddDays(7);
        var token = new RefreshToken(Guid.NewGuid(), "sample_token", expiresAt, now);

        // Act
        token.Revoke(now.AddHours(1));

        // Assert
        token.IsRevoked.Should().BeTrue();
        token.RevokedAt.Should().Be(now.AddHours(1));
        token.IsActive(now).Should().BeFalse();
    }
}

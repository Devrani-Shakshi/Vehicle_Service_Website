using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ServicePlatform.Data;
using ServicePlatform.Models;
using ServicePlatform.Services;
using Xunit;

namespace ServicePlatform.Tests.Services;

public class OtpServiceTests
{
    private ApplicationDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GenerateOtpAsync_ShouldCreateNewOtp()
    {
        // Arrange
        var context = GetDbContext();
        var loggerMock = new Mock<ILogger<OtpService>>();
        var service = new OtpService(context, loggerMock.Object);
        var email = "test@example.com";

        // Act
        var otp = await service.GenerateOtpAsync(email);

        // Assert
        Assert.NotNull(otp);
        Assert.Equal(6, otp.Length);
        var entry = await context.OtpVerifications.FirstOrDefaultAsync(o => o.Email == email);
        Assert.NotNull(entry);
        Assert.Equal(otp, entry.OtpCode);
    }

    [Fact]
    public async Task VerifyOtpAsync_ShouldReturnTrue_WhenValid()
    {
        // Arrange
        var context = GetDbContext();
        var loggerMock = new Mock<ILogger<OtpService>>();
        var service = new OtpService(context, loggerMock.Object);
        var email = "test@example.com";
        var otp = await service.GenerateOtpAsync(email);

        // Act
        var result = await service.VerifyOtpAsync(email, otp);

        // Assert
        Assert.True(result);
        var entry = await context.OtpVerifications.FirstOrDefaultAsync(o => o.Email == email);
        Assert.True(entry!.IsUsed);
    }

    [Fact]
    public async Task VerifyOtpAsync_ShouldReturnFalse_WhenExpired()
    {
        // Arrange
        var context = GetDbContext();
        var loggerMock = new Mock<ILogger<OtpService>>();
        var service = new OtpService(context, loggerMock.Object);
        var email = "test@example.com";
        
        var otp = "123456";
        await context.OtpVerifications.AddAsync(new OtpVerification
        {
            Email = email,
            OtpCode = otp,
            ExpiryTime = DateTime.UtcNow.AddMinutes(-1), // Expired
            IsUsed = false
        });
        await context.SaveChangesAsync();

        // Act
        var result = await service.VerifyOtpAsync(email, otp);

        // Assert
        Assert.False(result);
    }
}

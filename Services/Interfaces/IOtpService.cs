namespace ServicePlatform.Services.Interfaces;

public interface IOtpService
{
    Task<string> GenerateOtpAsync(string email);
    Task<bool> VerifyOtpAsync(string email, string otp);
    Task<bool> IsVerifiedAsync(string email);
    Task MarkAsVerifiedAsync(string email);
    Task ClearVerificationAsync(string email);
}

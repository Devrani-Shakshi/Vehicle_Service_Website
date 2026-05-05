using Microsoft.EntityFrameworkCore;
using ServicePlatform.Data;
using ServicePlatform.Models;
using ServicePlatform.Services.Interfaces;

namespace ServicePlatform.Services;

public class OtpService : IOtpService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OtpService> _logger;

    public OtpService(ApplicationDbContext context, ILogger<OtpService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string> GenerateOtpAsync(string email)
    {
        // Deactivate old OTPs for this email
        var oldOtps = await _context.OtpVerifications
            .Where(o => o.Email == email && !o.IsUsed)
            .ToListAsync();
        
        foreach (var old in oldOtps) old.IsUsed = true;

        var otp = new Random().Next(100000, 999999).ToString();
        var entry = new OtpVerification
        {
            Email = email,
            OtpCode = otp,
            ExpiryTime = DateTime.UtcNow.AddMinutes(10),
            IsUsed = false
        };

        await _context.OtpVerifications.AddAsync(entry);
        await _context.SaveChangesAsync();
        
        return otp;
    }

    public async Task<bool> VerifyOtpAsync(string email, string otp)
    {
        var entry = await _context.OtpVerifications
            .Where(o => o.Email == email && o.OtpCode == otp && !o.IsUsed && o.ExpiryTime > DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (entry == null) return false;

        entry.IsUsed = true;
        _context.OtpVerifications.Update(entry);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IsVerifiedAsync(string email)
    {
        // We use a specialized "Verified" entry or check the latest used OTP within a short window
        // For simplicity, we'll use a specific record or just a boolean in the service (but for DB-backed, we need a record)
        return await _context.OtpVerifications.AnyAsync(o => o.Email == email && o.IsUsed && o.CreatedAt > DateTime.UtcNow.AddMinutes(-30));
    }

    public async Task MarkAsVerifiedAsync(string email)
    {
        // Already handled by VerifyOtpAsync marking it as used
        // But we could add a special 'Verified' record if needed
    }

    public async Task ClearVerificationAsync(string email)
    {
        var entries = await _context.OtpVerifications.Where(o => o.Email == email).ToListAsync();
        foreach (var e in entries) e.IsUsed = true;
        await _context.SaveChangesAsync();
    }
}

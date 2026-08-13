using FitHub.Application.Equipment;
using FitHub.Domain.Equipment;
using FitHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace FitHub.Infrastructure.EquipmentServices;
public sealed class DeviceAuthenticator(FitHubDbContext dbContext, IPasswordHasher<EquipmentDevice> passwordHasher) : IDeviceAuthenticator
{
    public async Task<bool> AuthenticateAsync(Guid equipmentId, string deviceCode, string credential, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceCode) || string.IsNullOrWhiteSpace(credential)) return false;
        var device = await dbContext.EquipmentDevices.SingleOrDefaultAsync(x => x.EquipmentId == equipmentId && x.DeviceCode == deviceCode, cancellationToken);
        return device is not null && passwordHasher.VerifyHashedPassword(device, device.CredentialHash, credential) != PasswordVerificationResult.Failed;
    }
}

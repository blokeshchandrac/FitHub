using FitHub.Application.Parking;using FitHub.Domain.Parking;using FitHub.Infrastructure.Persistence;using Microsoft.AspNetCore.Identity;using Microsoft.EntityFrameworkCore;
namespace FitHub.Infrastructure.ParkingServices;
public sealed class ParkingCameraAuthenticator(FitHubDbContext db,IPasswordHasher<ParkingCamera> hasher):IParkingCameraAuthenticator
{
    public async Task<bool> AuthenticateAsync(string cameraCode,string credential,CancellationToken ct=default){if(string.IsNullOrWhiteSpace(cameraCode)||string.IsNullOrWhiteSpace(credential))return false;var camera=await db.ParkingCameras.SingleOrDefaultAsync(x=>x.CameraCode==cameraCode&&x.IsActive,ct);return camera is not null&&hasher.VerifyHashedPassword(camera,camera.CredentialHash,credential)!=PasswordVerificationResult.Failed;}
}

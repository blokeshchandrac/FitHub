using FitHub.Application.FaultReports;
using Microsoft.Extensions.Configuration;
namespace FitHub.Infrastructure.FaultReports;
public sealed class LocalPhotoStorage(IConfiguration configuration) : IPhotoStorage
{
    public async Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken = default)
    {
        var root = configuration["FaultPhotos:LocalPath"] ?? Path.Combine(AppContext.BaseDirectory, "App_Data", "fault-images");
        Directory.CreateDirectory(root);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        await using var output = File.Create(Path.Combine(root, fileName));
        await content.CopyToAsync(output, cancellationToken);
        return $"/fault-images/{fileName}";
    }
}

using GalleryApi.Domain.Interfaces;
using GalleryApi.Infrastructure.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace GalleryApi.Infrastructure.Storage;

public class LocalStorageService : IStorageService
{
    private readonly string _basePath;

    public LocalStorageService(IWebHostEnvironment env, IOptions<StorageOptions> opts)
    {
        _basePath = Path.Combine(env.ContentRootPath, opts.Value.BasePath);
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, Guid albumId)
    {
        var albumDir = Path.Combine(_basePath, albumId.ToString());
        Directory.CreateDirectory(albumDir);

        var filePath = Path.Combine(albumDir, fileName);
        using var output = File.Create(filePath);
        await fileStream.CopyToAsync(output);

        return $"/uploads/{albumId}/{fileName}";
    }

    public Task DeleteAsync(string fileName, Guid albumId)
    {
        var filePath = Path.Combine(_basePath, albumId.ToString(), fileName);
        if (File.Exists(filePath))
            File.Delete(filePath);

        return Task.CompletedTask;
    }
}

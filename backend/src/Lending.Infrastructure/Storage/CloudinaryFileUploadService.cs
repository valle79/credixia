using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;

namespace Lending.Infrastructure.Storage;

public sealed class CloudinaryFileUploadService : IFileUploadService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryFileUploadService(IOptions<CloudinaryOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        
        var opts = options.Value;
        
        if (string.IsNullOrEmpty(opts.CloudName) || 
            string.IsNullOrEmpty(opts.ApiKey) || 
            string.IsNullOrEmpty(opts.ApiSecret))
        {
            throw new InvalidOperationException("Cloudinary configuration is missing or incomplete.");
        }

        var account = new Account(opts.CloudName, opts.ApiKey, opts.ApiSecret);
        _cloudinary = new Cloudinary(account);
        _cloudinary.Api.Secure = true;
    }

    public async Task<string> UploadImageAsync(
        Stream imageStream, 
        string fileName, 
        string folder, 
        CancellationToken cancellationToken = default)
    {
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, imageStream),
            Folder = folder,
            Transformation = new Transformation()
                .Width(800)
                .Height(800)
                .Crop("limit")
                .Quality("auto:good")
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

        if (uploadResult.StatusCode != System.Net.HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"Failed to upload image: {uploadResult.Error?.Message}");
        }

        return uploadResult.SecureUrl.ToString();
    }

    public async Task<bool> DeleteImageAsync(string publicId, CancellationToken cancellationToken = default)
    {
        var deletionParams = new DeletionParams(publicId);
        var result = await _cloudinary.DestroyAsync(deletionParams);
        return result.Result == "ok";
    }
}

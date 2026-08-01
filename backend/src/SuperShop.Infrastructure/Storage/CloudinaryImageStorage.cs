using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Logging;
using SuperShop.Application.Admin;

namespace SuperShop.Infrastructure.Storage;

public class CloudinaryImageStorage(Cloudinary cloudinary, ILogger<CloudinaryImageStorage> logger) : IImageStorage
{
    private const string Folder = "supershop/produtos";

    public async Task<StoredImage> UploadAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var parameters = new ImageUploadParams
        {
            File = new FileDescription(fileName, content),
            Folder = Folder,
            UseFilename = true,
            UniqueFilename = true,
            Overwrite = false
        };

        var result = await cloudinary.UploadAsync(parameters, cancellationToken);

        if (result.Error is not null)
        {
            logger.LogError("Cloudinary recusou o upload de {FileName}: {Message}", fileName, result.Error.Message);

            throw new InvalidOperationException($"O carregamento da imagem falhou: {result.Error.Message}");
        }

        logger.LogInformation("Imagem carregada como {PublicId}.", result.PublicId);

        return new StoredImage(result.PublicId, result.SecureUrl.ToString());
    }

    public async Task DeleteAsync(string publicId, CancellationToken cancellationToken = default)
    {
        var result = await cloudinary.DestroyAsync(new DeletionParams(publicId));

        if (result.Error is not null)
        {
            logger.LogWarning(
                "Cloudinary não removeu {PublicId}: {Message}. O registo foi apagado na mesma.",
                publicId, result.Error.Message);
        }
    }
}

public class UnavailableImageStorage : IImageStorage
{
    private const string Message =
        "Cloudinary:Url is not configured. Set it with dotnet user-secrets in development, " +
        "or as an environment variable in production.";

    public Task<StoredImage> UploadAsync(Stream content, string fileName, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(Message);

    public Task DeleteAsync(string publicId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

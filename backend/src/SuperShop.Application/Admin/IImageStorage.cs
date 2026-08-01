namespace SuperShop.Application.Admin;

public record StoredImage(string PublicId, string Url);

public interface IImageStorage
{
    Task<StoredImage> UploadAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string publicId, CancellationToken cancellationToken = default);
}

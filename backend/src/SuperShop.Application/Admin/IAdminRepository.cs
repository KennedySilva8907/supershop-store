using SuperShop.Domain.Enums;

namespace SuperShop.Application.Admin;

public interface IAdminRepository
{
    Task<IReadOnlyList<AdminProductDto>> ListProductsAsync(string? search, CancellationToken cancellationToken);

    Task<AdminProductDto> CreateProductAsync(SaveProductRequest request, CancellationToken cancellationToken);

    Task<AdminProductDto> UpdateProductAsync(int id, SaveProductRequest request, CancellationToken cancellationToken);

    Task<AdminProductDto> SetProductStatusAsync(int id, bool isActive, CancellationToken cancellationToken);

    Task<IReadOnlyList<AdminVariantDto>> ListVariantsAsync(int productId, CancellationToken cancellationToken);

    Task<AdminVariantDto> SetStockAsync(int variantId, int stock, CancellationToken cancellationToken);

    Task<IReadOnlyList<AdminImageDto>> ListImagesAsync(int productId, CancellationToken cancellationToken);

    Task<AdminImageDto> AddImageAsync(int productId, string publicId, string altText, CancellationToken cancellationToken);

    Task<string> RemoveImageAsync(int productId, int imageId, CancellationToken cancellationToken);

    Task<IReadOnlyList<AdminOrderDto>> ListOrdersAsync(OrderStatus? status, CancellationToken cancellationToken);

    Task<AdminProductFormDto> GetProductAsync(int id, CancellationToken cancellationToken);

    Task<AdminImageDto> SetPrimaryImageAsync(int productId, int imageId, CancellationToken cancellationToken);

    Task<AdminOrderDetailDto> GetOrderAsync(int orderId, CancellationToken cancellationToken);

    Task<AdminOrderDto> SetOrderStatusAsync(int orderId, OrderStatus status, CancellationToken cancellationToken);

    Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken);
}

public class AdminService(IAdminRepository repository, IImageStorage storage)
{
    public const int LowStockThreshold = 5;

    public Task<IReadOnlyList<AdminProductDto>> ListProductsAsync(
        string? search,
        CancellationToken cancellationToken = default) =>
        repository.ListProductsAsync(string.IsNullOrWhiteSpace(search) ? null : search.Trim(), cancellationToken);

    public Task<AdminProductDto> CreateProductAsync(
        SaveProductRequest request,
        CancellationToken cancellationToken = default) =>
        repository.CreateProductAsync(Clean(request), cancellationToken);

    public Task<AdminProductDto> UpdateProductAsync(
        int id,
        SaveProductRequest request,
        CancellationToken cancellationToken = default) =>
        repository.UpdateProductAsync(id, Clean(request), cancellationToken);

    public Task<AdminProductDto> SetProductStatusAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default) =>
        repository.SetProductStatusAsync(id, isActive, cancellationToken);

    public Task<IReadOnlyList<AdminVariantDto>> ListVariantsAsync(
        int productId,
        CancellationToken cancellationToken = default) =>
        repository.ListVariantsAsync(productId, cancellationToken);

    public Task<AdminVariantDto> SetStockAsync(
        int variantId,
        int stock,
        CancellationToken cancellationToken = default) =>
        repository.SetStockAsync(variantId, Math.Max(0, stock), cancellationToken);

    public Task<IReadOnlyList<AdminImageDto>> ListImagesAsync(
        int productId,
        CancellationToken cancellationToken = default) =>
        repository.ListImagesAsync(productId, cancellationToken);

    public async Task<AdminImageDto> UploadImageAsync(
        int productId,
        Stream content,
        string fileName,
        string altText,
        CancellationToken cancellationToken = default)
    {
        var stored = await storage.UploadAsync(content, fileName, cancellationToken);

        return await repository.AddImageAsync(productId, stored.PublicId, altText.Trim(), cancellationToken);
    }

    public async Task RemoveImageAsync(
        int productId,
        int imageId,
        CancellationToken cancellationToken = default)
    {
        var publicId = await repository.RemoveImageAsync(productId, imageId, cancellationToken);

        await storage.DeleteAsync(publicId, cancellationToken);
    }

    public Task<IReadOnlyList<AdminOrderDto>> ListOrdersAsync(
        OrderStatus? status,
        CancellationToken cancellationToken = default) =>
        repository.ListOrdersAsync(status, cancellationToken);

    public Task<AdminProductFormDto> GetProductAsync(int id, CancellationToken cancellationToken = default) =>
        repository.GetProductAsync(id, cancellationToken);

    public Task<AdminImageDto> SetPrimaryImageAsync(
        int productId,
        int imageId,
        CancellationToken cancellationToken = default) =>
        repository.SetPrimaryImageAsync(productId, imageId, cancellationToken);

    public Task<AdminOrderDetailDto> GetOrderAsync(int orderId, CancellationToken cancellationToken = default) =>
        repository.GetOrderAsync(orderId, cancellationToken);

    public Task<AdminOrderDto> SetOrderStatusAsync(
        int orderId,
        OrderStatus status,
        CancellationToken cancellationToken = default) =>
        repository.SetOrderStatusAsync(orderId, status, cancellationToken);

    public Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default) =>
        repository.GetDashboardAsync(cancellationToken);

    private static SaveProductRequest Clean(SaveProductRequest request) => request with
    {
        Name = request.Name.Trim(),
        Slug = request.Slug.Trim().ToLowerInvariant().Replace(' ', '-'),
        Description = request.Description.Trim()
    };
}

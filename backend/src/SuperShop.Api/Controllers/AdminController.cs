using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperShop.Application.Admin;
using SuperShop.Domain.Enums;

namespace SuperShop.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = "RequireAdmin")]
public class AdminController(AdminService admin) : ControllerBase
{
    private const long MaxImageBytes = 5 * 1024 * 1024;

    private static readonly string[] AllowedImageTypes =
        ["image/png", "image/jpeg", "image/webp", "image/avif"];

    [HttpGet("products")]
    public async Task<ActionResult<IReadOnlyList<AdminProductDto>>> ListProducts(
        [FromQuery] string? search,
        CancellationToken cancellationToken) =>
        Ok(await admin.ListProductsAsync(search, cancellationToken));

    [HttpGet("products/{id:int}")]
    public async Task<ActionResult<AdminProductFormDto>> GetProduct(
        int id,
        CancellationToken cancellationToken) =>
        Ok(await admin.GetProductAsync(id, cancellationToken));

    [HttpPost("products")]
    public async Task<ActionResult<AdminProductDto>> CreateProduct(
        SaveProductRequest request,
        CancellationToken cancellationToken)
    {
        var created = await admin.CreateProductAsync(request, cancellationToken);

        return CreatedAtAction(nameof(ListProducts), new { id = created.Id }, created);
    }

    [HttpPut("products/{id:int}")]
    public async Task<ActionResult<AdminProductDto>> UpdateProduct(
        int id,
        SaveProductRequest request,
        CancellationToken cancellationToken) =>
        Ok(await admin.UpdateProductAsync(id, request, cancellationToken));

    [HttpPatch("products/{id:int}/status")]
    public async Task<ActionResult<AdminProductDto>> SetProductStatus(
        int id,
        SetProductStatusRequest request,
        CancellationToken cancellationToken) =>
        Ok(await admin.SetProductStatusAsync(id, request.IsActive, cancellationToken));

    [HttpGet("products/{id:int}/variants")]
    public async Task<ActionResult<IReadOnlyList<AdminVariantDto>>> ListVariants(
        int id,
        CancellationToken cancellationToken) =>
        Ok(await admin.ListVariantsAsync(id, cancellationToken));

    [HttpPut("variants/{id:int}/stock")]
    public async Task<ActionResult<AdminVariantDto>> SetStock(
        int id,
        SetStockRequest request,
        CancellationToken cancellationToken) =>
        Ok(await admin.SetStockAsync(id, request.Stock, cancellationToken));

    [HttpGet("products/{id:int}/images")]
    public async Task<ActionResult<IReadOnlyList<AdminImageDto>>> ListImages(
        int id,
        CancellationToken cancellationToken) =>
        Ok(await admin.ListImagesAsync(id, cancellationToken));

    [HttpPost("products/{id:int}/images")]
    [RequestSizeLimit(MaxImageBytes)]
    public async Task<ActionResult<AdminImageDto>> UploadImage(
        int id,
        IFormFile file,
        [FromForm] string altText,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest(new ProblemDetails { Title = "Ficheiro vazio.", Status = 400 });
        }

        if (!AllowedImageTypes.Contains(file.ContentType))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Formato não suportado.",
                Detail = "Usa PNG, JPEG, WebP ou AVIF.",
                Status = 400
            });
        }

        await using var stream = file.OpenReadStream();

        return Ok(await admin.UploadImageAsync(id, stream, file.FileName, altText, cancellationToken));
    }

    [HttpPatch("products/{id:int}/images/{imageId:int}/primary")]
    public async Task<ActionResult<AdminImageDto>> SetPrimaryImage(
        int id,
        int imageId,
        CancellationToken cancellationToken) =>
        Ok(await admin.SetPrimaryImageAsync(id, imageId, cancellationToken));

    [HttpDelete("products/{id:int}/images/{imageId:int}")]
    public async Task<IActionResult> RemoveImage(int id, int imageId, CancellationToken cancellationToken)
    {
        await admin.RemoveImageAsync(id, imageId, cancellationToken);

        return NoContent();
    }

    [HttpGet("orders")]
    public async Task<ActionResult<IReadOnlyList<AdminOrderDto>>> ListOrders(
        [FromQuery] OrderStatus? status,
        CancellationToken cancellationToken) =>
        Ok(await admin.ListOrdersAsync(status, cancellationToken));

    [HttpGet("orders/{id:int}")]
    public async Task<ActionResult<AdminOrderDetailDto>> GetOrder(
        int id,
        CancellationToken cancellationToken) =>
        Ok(await admin.GetOrderAsync(id, cancellationToken));

    [HttpPatch("orders/{id:int}/status")]
    public async Task<ActionResult<AdminOrderDto>> SetOrderStatus(
        int id,
        SetOrderStatusRequest request,
        CancellationToken cancellationToken) =>
        Ok(await admin.SetOrderStatusAsync(id, request.Status, cancellationToken));

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardDto>> Dashboard(CancellationToken cancellationToken) =>
        Ok(await admin.GetDashboardAsync(cancellationToken));
}

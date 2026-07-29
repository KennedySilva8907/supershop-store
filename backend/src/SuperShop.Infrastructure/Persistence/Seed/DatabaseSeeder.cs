using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SuperShop.Domain.Entities;
using SuperShop.Domain.Enums;
using SuperShop.Infrastructure.Identity;

namespace SuperShop.Infrastructure.Persistence.Seed;

public class DatabaseSeeder(
    SuperShopDbContext context,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IConfiguration configuration,
    ILogger<DatabaseSeeder> logger)
{
    private const int StockSeed = 20260729;

    private static readonly (string Name, string Slug, SizeSystem System, int Order)[] Categories =
    [
        ("Sapatilhas", "sapatilhas", SizeSystem.Footwear, 1),
        ("T-shirts", "t-shirts", SizeSystem.Apparel, 2),
        ("Casacos", "casacos", SizeSystem.Apparel, 3),
        ("Calças", "calcas", SizeSystem.Apparel, 4),
        ("Calções", "calcoes", SizeSystem.Apparel, 5)
    ];

    private static readonly (string Name, string Slug)[] Collections =
    [
        ("AXIS", "axis"),
        ("CORE", "core")
    ];

    private static readonly string[] FootwearSizes = ["38", "39", "40", "41", "42", "43", "44", "45", "46"];
    private static readonly string[] ApparelSizes = ["XS", "S", "M", "L", "XL", "XXL"];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedRolesAsync();
        await SeedAdminAsync();
        await SeedCategoriesAsync(cancellationToken);
        await SeedCollectionsAsync(cancellationToken);
        await SeedSizesAsync(cancellationToken);
        await SeedProductsAsync(cancellationToken);
    }

    private async Task SeedRolesAsync()
    {
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private async Task SeedAdminAsync()
    {
        var email = configuration["Admin:Email"];
        var password = configuration["Admin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "Admin:Email or Admin:Password not configured. Skipping admin user. " +
                "Set them with dotnet user-secrets to create the administrator.");
            return;
        }

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = "Admin",
            LastName = "SuperShop",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var result = await userManager.CreateAsync(admin, password);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create the administrator: {errors}");
        }

        await userManager.AddToRoleAsync(admin, Roles.Admin);
        logger.LogInformation("Administrator created for {Email}.", email);
    }

    private async Task SeedCategoriesAsync(CancellationToken cancellationToken)
    {
        foreach (var (name, slug, system, order) in Categories)
        {
            if (await context.Categories.AnyAsync(c => c.Slug == slug, cancellationToken))
            {
                continue;
            }

            context.Categories.Add(new Category
            {
                Name = name,
                Slug = slug,
                SizeSystem = system,
                DisplayOrder = order
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedCollectionsAsync(CancellationToken cancellationToken)
    {
        foreach (var (name, slug) in Collections)
        {
            if (await context.Collections.AnyAsync(c => c.Slug == slug, cancellationToken))
            {
                continue;
            }

            context.Collections.Add(new Collection { Name = name, Slug = slug });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedSizesAsync(CancellationToken cancellationToken)
    {
        await AddSizesAsync(SizeSystem.Footwear, FootwearSizes, cancellationToken);
        await AddSizesAsync(SizeSystem.Apparel, ApparelSizes, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task AddSizesAsync(SizeSystem system, string[] labels, CancellationToken cancellationToken)
    {
        for (var i = 0; i < labels.Length; i++)
        {
            var label = labels[i];

            if (await context.Sizes.AnyAsync(s => s.SizeSystem == system && s.Label == label, cancellationToken))
            {
                continue;
            }

            context.Sizes.Add(new Size { SizeSystem = system, Label = label, SortOrder = i + 1 });
        }
    }

    private async Task SeedProductsAsync(CancellationToken cancellationToken)
    {
        var categories = await context.Categories.ToDictionaryAsync(c => c.Slug, cancellationToken);
        var collections = await context.Collections.ToDictionaryAsync(c => c.Slug, cancellationToken);
        var sizes = await context.Sizes.OrderBy(s => s.SortOrder).ToListAsync(cancellationToken);

        var random = new Random(StockSeed);
        var createdAt = new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero);

        foreach (var seed in CatalogSeedData.Products)
        {
            random.Next();

            if (await context.Products.AnyAsync(p => p.Slug == seed.Slug, cancellationToken))
            {
                continue;
            }

            var category = categories[seed.CategorySlug];
            var collection = collections[seed.CollectionSlug];

            var product = new Product
            {
                Name = seed.Name,
                Slug = seed.Slug,
                Description = seed.Description,
                Price = seed.Price,
                CompareAtPrice = seed.CompareAtPrice,
                CategoryId = category.Id,
                CollectionId = collection.Id,
                IsActive = true,
                IsFeatured = seed.IsFeatured,
                CreatedAt = createdAt.AddDays(seed.Number)
            };

            foreach (var size in sizes.Where(s => s.SizeSystem == category.SizeSystem))
            {
                product.Variants.Add(new ProductVariant
                {
                    SizeId = size.Id,
                    Sku = $"SS-{seed.SkuPrefix}-{seed.Number:D3}-{size.Label}",
                    Stock = seed.IsSoldOut ? 0 : random.Next(0, 26)
                });
            }

            product.Images.Add(new ProductImage
            {
                PublicId = seed.ImagePublicId,
                AltText = seed.AltText,
                IsPrimary = true,
                SortOrder = 1
            });

            context.Products.Add(product);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}

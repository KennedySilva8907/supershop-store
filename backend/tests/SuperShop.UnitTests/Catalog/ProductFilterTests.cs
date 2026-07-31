using SuperShop.Application.Catalog;

namespace SuperShop.UnitTests.Catalog;

public class ProductFilterTests
{
    [Theory]
    [InlineData(100000, 48)]
    [InlineData(49, 48)]
    [InlineData(48, 48)]
    [InlineData(24, 24)]
    [InlineData(1, 1)]
    [InlineData(0, 12)]
    [InlineData(-3, 12)]
    public void Page_size_is_capped_and_never_below_one(int requested, int expected)
    {
        Assert.Equal(expected, new ProductFilter { PageSize = requested }.Normalised().PageSize);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(1, 1)]
    [InlineData(7, 7)]
    public void Page_never_falls_below_one(int requested, int expected)
    {
        Assert.Equal(expected, new ProductFilter { Page = requested }.Normalised().Page);
    }

    [Fact]
    public void Category_is_trimmed_and_lowercased()
    {
        Assert.Equal("sapatilhas", new ProductFilter { Category = "  Sapatilhas  " }.Normalised().Category);
    }

    [Fact]
    public void Size_is_uppercased_so_xl_matches_XL()
    {
        Assert.Equal("XL", new ProductFilter { Size = " xl " }.Normalised().Size);
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("   ", 0)]
    [InlineData("axis", 1)]
    [InlineData("axis,core", 2)]
    [InlineData(" AXIS , core ", 2)]
    [InlineData("axis,axis", 1)]
    [InlineData("axis,,core", 2)]
    public void Collection_splits_on_commas_trims_and_deduplicates(string value, int expected)
    {
        Assert.Equal(expected, new ProductFilter { Collection = value }.CollectionSlugs.Count);
    }

    [Fact]
    public void Blank_text_filters_become_null_so_they_are_not_applied()
    {
        var filter = new ProductFilter { Category = "  ", Search = "", Size = "   " }.Normalised();

        Assert.Null(filter.Category);
        Assert.Null(filter.Search);
        Assert.Null(filter.Size);
    }

    [Fact]
    public void Default_page_size_matches_the_documented_value()
    {
        Assert.Equal(12, ProductFilter.DefaultPageSize);
        Assert.Equal(48, ProductFilter.MaxPageSize);
    }
}

using Shouldly;
using System;
using Xunit;

namespace SmartPantry.Products;

public class Product_Tests
{
    [Fact]
    public void Should_Create_Valid_Product_And_Trim_Text()
    {
        var product = new Product(Guid.NewGuid(), "  Leche Entera  ", "  La Serenísima  ");
        product.Name.ShouldBe("Leche Entera");
        product.Brand.ShouldBe("La Serenísima");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Should_Throw_Exception_When_Name_Is_Invalid(string? invalidName)
    {
        Assert.Throws<ArgumentException>(() =>
        {
            new Product(Guid.NewGuid(), invalidName!, "Marca Valida");
        });
    }
}
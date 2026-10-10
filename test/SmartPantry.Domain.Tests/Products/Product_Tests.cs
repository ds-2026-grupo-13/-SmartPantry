using System;
using Shouldly;
using Xunit;

namespace SmartPantry.Products;

public class Product_Tests
{
    [Fact]
    public void Should_Create_Valid_Product_And_Trim_Text()
    {
        var product = new Product(Guid.NewGuid(), "  Leche Entera  ", "  La Serenísima  ", "  7790001234567  ");
        product.Name.ShouldBe("Leche Entera");
        product.Brand.ShouldBe("La Serenísima");
        product.Barcode.ShouldBe("7790001234567");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Should_Throw_Exception_When_Name_Is_Invalid(string? invalidName)
    {
        Assert.Throws<ArgumentException>(() =>
        {
            new Product(Guid.NewGuid(), invalidName!, "Marca Valida", "7790001234567");
        });
    }

    [Fact]
    public void Should_Update_Valid_Product_And_Trim_Text()
    {
        // Arrange: entidad inicial válida
        var product = new Product(Guid.NewGuid(), "Arroz", "Gallo", "7790001111111");

        // Act: modificación pasando datos con espacios en los extremos
        product.SetName("  Arroz Integral  ");
        product.SetBrand("  Molinos  ");
        product.SetBarcode("  7790002222222  ");

        // Assert: conserva la normalización (Trim)
        product.Name.ShouldBe("Arroz Integral");
        product.Brand.ShouldBe("Molinos");
        product.Barcode.ShouldBe("7790002222222");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Should_Reject_Invalid_Update_And_Keep_Previous_State(string? invalidName)
    {
        // Arrange: entidad inicial válida
        var product = new Product(Guid.NewGuid(), "Arroz", "Gallo", "7790001111111");

        // Act & Assert: la modificación con datos vacíos o nulos debe ser rechazada
        Assert.Throws<ArgumentException>(() =>
        {
            product.SetName(invalidName!);
        });

        // Verifica que no se produjeron cambios parciales y se mantuvo el estado previo
        product.Name.ShouldBe("Arroz");
        product.Brand.ShouldBe("Gallo");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Should_Throw_Exception_When_Barcode_Is_Invalid(string? invalidBarcode)
    {
        Assert.Throws<ArgumentException>(() =>
        {
            new Product(Guid.NewGuid(), "Producto Valido", "Marca Valida", invalidBarcode!);
        });
    }
}
using System;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp.Validation;
using Xunit;

namespace SmartPantry.Products;

public class ProductAppService_Tests : SmartPantryApplicationTestBase<SmartPantryApplicationTestModule>
{
    private readonly IProductAppService _productAppService;

    public ProductAppService_Tests()
    {
        _productAppService = GetRequiredService<IProductAppService>();
    }

    [Fact]
    public async Task Should_Create_And_Get_Product()
    {
        // Arrange
        var input = new CreateProductDto
        {
            Name = "Fideos Tallarines",
            Brand = "Matarazzo"
        };

        // Act
        var created = await _productAppService.CreateAsync(input);

        // Assert creación
        created.ShouldNotBeNull();
        created.Id.ShouldNotBe(Guid.Empty);
        created.Name.ShouldBe("Fideos Tallarines");

        // Act
        var retrieved = await _productAppService.GetAsync(created.Id);

        // Assert consulta
        retrieved.ShouldNotBeNull();
        retrieved.Id.ShouldBe(created.Id);
        retrieved.Name.ShouldBe("Fideos Tallarines");
    }

    [Fact]
    public async Task Should_Not_Create_Product_When_Name_Is_Missing()
    {
        var input = new CreateProductDto
        {
            Name = string.Empty,
            Brand = "Marca"
        };

        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _productAppService.CreateAsync(input);
        });
    }
}

using System;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;
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
            Brand = "Matarazzo",
            Barcode = "7790005551234"
        };

        // Act
        var created = await _productAppService.CreateAsync(input);

        // Assert creación
        created.ShouldNotBeNull();
        created.Id.ShouldNotBe(Guid.Empty);
        created.Name.ShouldBe("Fideos Tallarines");
        created.Barcode.ShouldBe("7790005551234");

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
            Brand = "Marca",
            Barcode = "7790001234567"
        };

        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _productAppService.CreateAsync(input);
        });
    }

    [Fact]
    public async Task Should_Execute_Full_Crud_Lifecycle()
    {
        // 1. Create (Registrar)
        var created = await _productAppService.CreateAsync(new CreateProductDto
        {
            Name = "Aceite de Girasol",
            Brand = "Natura",
            Barcode = "7790009998887"
        });
        created.ShouldNotBeNull();
        var id = created.Id;

        // 2. GetList (Listar paginado)
        var list = await _productAppService.GetListAsync(new PagedAndSortedResultRequestDto());
        list.TotalCount.ShouldBeGreaterThan(0);
        list.Items.ShouldContain(p => p.Id == id);

        // 3. Update (Modificar)
        var updated = await _productAppService.UpdateAsync(id, new UpdateProductDto
        {
            Name = "Aceite de Oliva",
            Brand = "Cocinero",
            Barcode = "7790001112223"
        });
        updated.Name.ShouldBe("Aceite de Oliva");
        updated.Brand.ShouldBe("Cocinero");
        updated.Barcode.ShouldBe("7790001112223");

        // 4. Get (Consultar)
        var retrieved = await _productAppService.GetAsync(id);
        retrieved.Name.ShouldBe("Aceite de Oliva");

        // 5. Delete (Eliminar)
        await _productAppService.DeleteAsync(id);

        // 6. Comprobar que consultar el Id eliminado arroja excepción de entidad no encontrada
        await Assert.ThrowsAsync<EntityNotFoundException>(async () =>
        {
            await _productAppService.GetAsync(id);
        });
    }
}
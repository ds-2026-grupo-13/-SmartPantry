using System;
using System.Threading.Tasks;
using Shouldly;
using SmartPantry.Products;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Validation;
using Xunit;

namespace SmartPantry.Pantry;

public class PantryAppService_Tests : SmartPantryApplicationTestBase<SmartPantryApplicationTestModule>
{
    private readonly IPantryAppService _pantryAppService;
    private readonly IRepository<PantryItem, Guid> _pantryItemRepository;
    private readonly IRepository<Product, Guid> _productRepository;

    public PantryAppService_Tests()
    {
        _pantryAppService = GetRequiredService<IPantryAppService>();
        _pantryItemRepository = GetRequiredService<IRepository<PantryItem, Guid>>();
        _productRepository = GetRequiredService<IRepository<Product, Guid>>();
    }

    [Fact]
    public async Task Should_Create_And_List_Only_Current_User_Pantry_Items_With_Product_Name()
    {
        // Arrange
        var product = await _productRepository.InsertAsync(
            new Product(Guid.NewGuid(), "Leche Descremada", "La Serenísima", "7790001009991"));

        var otherUserId = Guid.NewGuid();
        var otherUserItem = await _pantryItemRepository.InsertAsync(
            new PantryItem(Guid.NewGuid(), otherUserId, product.Id, DateTime.UtcNow.Date.AddDays(5), PantryItemStatus.Active, 1, "L"));

        // Act
        var created = await _pantryAppService.CreateAsync(new CreatePantryItemDto
        {
            ProductId = product.Id,
            Quantity = 2,
            Unit = "L",
            ExpirationDate = new DateTime(2026, 12, 15, 0, 0, 0, DateTimeKind.Utc)
        });

        var list = await _pantryAppService.GetListAsync(new GetPantryItemsInput());

        // Assert
        created.ShouldNotBeNull();
        created.ProductName.ShouldBe("Leche Descremada");
        created.Quantity.ShouldBe(2);
        created.Unit.ShouldBe("L");
        created.Status.ShouldBe(PantryItemStatus.Active);

        list.Items.ShouldContain(x => x.Id == created.Id && x.ProductName == "Leche Descremada");
        list.Items.ShouldNotContain(x => x.Id == otherUserItem.Id);
    }

    [Fact]
    public async Task Should_Update_Quantity_Unit_And_ExpirationDate()
    {
        // Arrange
        var product = await _productRepository.InsertAsync(
            new Product(Guid.NewGuid(), "Avena Instantánea", "Quaker", "7790002008882"));

        var created = await _pantryAppService.CreateAsync(new CreatePantryItemDto
        {
            ProductId = product.Id,
            Quantity = 1,
            Unit = "paquete",
            ExpirationDate = new DateTime(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc)
        });

        var newExpiration = new DateTime(2026, 12, 20, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var updated = await _pantryAppService.UpdateAsync(created.Id, new UpdatePantryItemDto
        {
            Quantity = 500,
            Unit = "g",
            ExpirationDate = newExpiration
        });

        // Assert
        updated.Quantity.ShouldBe(500);
        updated.Unit.ShouldBe("g");
        updated.ExpirationDate.ShouldBe(newExpiration);
    }

    [Fact]
    public async Task Should_Reject_Invalid_Update_Input()
    {
        // Arrange
        var product = await _productRepository.InsertAsync(
            new Product(Guid.NewGuid(), "Harina 0000", "Pureza", "7790003007773"));

        var created = await _pantryAppService.CreateAsync(new CreatePantryItemDto
        {
            ProductId = product.Id,
            Quantity = 1,
            Unit = "kg"
        });

        // Act & Assert: cantidad negativa rechazada por validación DTO
        await Should.ThrowAsync<AbpValidationException>(async () =>
        {
            await _pantryAppService.UpdateAsync(created.Id, new UpdatePantryItemDto
            {
                Quantity = -5,
                Unit = "kg"
            });
        });

        // Act & Assert: fecha fuera de rango rechazada por validación DTO
        await Should.ThrowAsync<AbpValidationException>(async () =>
        {
            await _pantryAppService.UpdateAsync(created.Id, new UpdatePantryItemDto
            {
                Quantity = 1,
                Unit = "kg",
                ExpirationDate = new DateTime(1900, 1, 1)
            });
        });
    }

    [Fact]
    public async Task Should_Mark_As_Consumed_And_Delete_Own_Item()
    {
        // Arrange
        var product = await _productRepository.InsertAsync(
            new Product(Guid.NewGuid(), "Mermelada Durazno", "Arcor", "7790004006664"));

        var created = await _pantryAppService.CreateAsync(new CreatePantryItemDto
        {
            ProductId = product.Id,
            Quantity = 1,
            Unit = "frasco"
        });

        // Act 1: Marcar como consumido (no elimina automáticamente según Regla de Negocio 6)
        var consumed = await _pantryAppService.MarkAsConsumedAsync(created.Id);
        consumed.Status.ShouldBe(PantryItemStatus.Consumed);

        // Act 2: Eliminar explícitamente
        await _pantryAppService.DeleteAsync(created.Id);

        // Assert: Ya no existe al consultarlo
        await Should.ThrowAsync<EntityNotFoundException>(async () =>
        {
            await _pantryAppService.GetAsync(created.Id);
        });
    }

    [Fact]
    public async Task Should_Not_Allow_Reading_Modifying_Consuming_Or_Deleting_Another_Users_Item()
    {
        // Arrange: Ítem perteneciente a otro usuario
        var otherUserId = Guid.NewGuid();
        var product = await _productRepository.InsertAsync(
            new Product(Guid.NewGuid(), "Café Molido", "Cabrales", "7790005005555"));

        var foreignItem = await _pantryItemRepository.InsertAsync(
            new PantryItem(
                Guid.NewGuid(),
                otherUserId,
                product.Id,
                DateTime.UtcNow.Date.AddDays(30),
                PantryItemStatus.Active,
                250,
                "g"));

        // Act & Assert: Todas las operaciones deben responder 404 (EntityNotFoundException)
        await Should.ThrowAsync<EntityNotFoundException>(async () =>
        {
            await _pantryAppService.GetAsync(foreignItem.Id);
        });

        await Should.ThrowAsync<EntityNotFoundException>(async () =>
        {
            await _pantryAppService.UpdateAsync(foreignItem.Id, new UpdatePantryItemDto
            {
                Quantity = 999,
                Unit = "g"
            });
        });

        await Should.ThrowAsync<EntityNotFoundException>(async () =>
        {
            await _pantryAppService.MarkAsConsumedAsync(foreignItem.Id);
        });

        await Should.ThrowAsync<EntityNotFoundException>(async () =>
        {
            await _pantryAppService.DeleteAsync(foreignItem.Id);
        });

        // Verificar que el ítem del otro usuario permanece intacto
        var unchanged = await _pantryItemRepository.GetAsync(foreignItem.Id);
        unchanged.Quantity.ShouldBe(250);
        unchanged.Status.ShouldBe(PantryItemStatus.Active);
    }
}


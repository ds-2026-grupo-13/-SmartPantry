using System;
using Shouldly;
using Xunit;

namespace SmartPantry.Pantry;

public class PantryItem_Tests
{
    [Fact]
    public void Should_Create_Valid_PantryItem()
    {
        var ownerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var expirationDate = new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc);

        var item = new PantryItem(Guid.NewGuid(), ownerId, productId, expirationDate, PantryItemStatus.Active);

        item.OwnerId.ShouldBe(ownerId);
        item.ProductId.ShouldBe(productId);
        item.Status.ShouldBe(PantryItemStatus.Active);
        item.ExpirationDate.ShouldBe(expirationDate);
    }

    [Fact]
    public void Should_Throw_When_OwnerId_Is_Empty()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            new PantryItem(Guid.NewGuid(), Guid.Empty, Guid.NewGuid());
        });
    }

    [Fact]
    public void Should_Throw_When_ProductId_Is_Empty()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            new PantryItem(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty);
        });
    }

    [Fact]
    public void Should_Normalize_Expiration_Date_To_Utc_Without_Time()
    {
        var item = new PantryItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var dateWithTime = new DateTime(2026, 11, 5, 14, 30, 0);

        item.SetExpirationDate(dateWithTime);

        item.ExpirationDate.ShouldNotBeNull();
        item.ExpirationDate.Value.Hour.ShouldBe(0);
        item.ExpirationDate.Value.Minute.ShouldBe(0);
        item.ExpirationDate.Value.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    public void Should_Mark_As_Consumed_And_Reactivate()
    {
        var item = new PantryItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        item.Status.ShouldBe(PantryItemStatus.Active);

        item.MarkAsConsumed();
        item.Status.ShouldBe(PantryItemStatus.Consumed);

        item.Activate();
        item.Status.ShouldBe(PantryItemStatus.Active);
    }

    [Fact]
    public void Should_Update_Deactivate_And_Reactivate_ExpirationWarning()
    {
        var warningId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var expirationDate = now.AddDays(2);

        var warning = new ExpirationWarning(warningId, itemId, WarningType.Expiration, expirationDate, now, "Aviso inicial", isActive: true);

        warning.IsActive.ShouldBeTrue();
        warning.Message.ShouldBe("Aviso inicial");

        // Deactivate
        warning.Deactivate(now.AddHours(1));
        warning.IsActive.ShouldBeFalse();

        // Reactivate
        var newExpiration = now.AddDays(1);
        warning.Reactivate(newExpiration, now.AddHours(2), "Aviso reactivado");
        warning.IsActive.ShouldBeTrue();
        warning.ExpirationDate.ShouldBe(newExpiration.Date);
        warning.Message.ShouldBe("Aviso reactivado");
    }

    [Fact]
    public void Should_Reactivate_Deactivated_Warning_When_Item_Returns_To_Threshold()
    {
        var warningId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Advertencia previamente desactivada
        var warning = new ExpirationWarning(
            warningId,
            itemId,
            WarningType.Expiration,
            now.AddDays(15),
            now.AddDays(-1),
            "Aviso anterior desactivado",
            isActive: false);

        warning.IsActive.ShouldBeFalse();

        // Reactivación al volver a estar dentro del umbral (vence en 2 días)
        var newExpiration = now.AddDays(2);
        warning.Reactivate(newExpiration, now, "Producto próximo a vencer (dentro del umbral)");

        warning.IsActive.ShouldBeTrue();
        warning.ExpirationDate.ShouldBe(newExpiration.Date);
        warning.Message.ShouldNotBeNull();
        warning.Message.ShouldContain("próximo a vencer");
    }
}

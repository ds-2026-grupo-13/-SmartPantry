using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace SmartPantry.Pantry;

[Collection(SmartPantryTestConsts.CollectionDefinitionName)]
public class ExpirationWarningManager_Tests : SmartPantry.EntityFrameworkCore.SmartPantryEntityFrameworkCoreTestBase
{
    private readonly IExpirationWarningManager _warningManager;
    private readonly IRepository<PantryItem, Guid> _pantryItemRepository;
    private readonly IRepository<ExpirationWarning, Guid> _warningRepository;

    public ExpirationWarningManager_Tests()
    {
        _warningManager = GetRequiredService<IExpirationWarningManager>();
        _pantryItemRepository = GetRequiredService<IRepository<PantryItem, Guid>>();
        _warningRepository = GetRequiredService<IRepository<ExpirationWarning, Guid>>();
    }

    private async Task ClearDataAsync()
    {
        var warnings = await _warningRepository.GetListAsync();
        foreach (var w in warnings)
        {
            await _warningRepository.DeleteAsync(w);
        }

        var items = await _pantryItemRepository.GetListAsync();
        foreach (var item in items)
        {
            await _pantryItemRepository.DeleteAsync(item);
        }
    }

    [Fact]
    public async Task Should_Process_Warnings_With_Controlled_Utc_Date_Covering_All_Cases()
    {
        await ClearDataAsync();

        // Arrange: Fecha controlada explícitamente en UTC
        var referenceDateUtc = new DateTime(2026, 10, 14, 12, 0, 0, DateTimeKind.Utc);
        var baseDate = referenceDateUtc.Date; // 2026-10-14
        var ownerId = Guid.NewGuid();

        // 1. Dentro del umbral (vence en 2 días: 2026-10-16 <= 2026-10-17) -> Debe generar advertencia
        var itemWithinThreshold = await _pantryItemRepository.InsertAsync(
            new PantryItem(Guid.NewGuid(), ownerId, Guid.NewGuid(), baseDate.AddDays(2), PantryItemStatus.Active));

        // 2. Día límite del umbral (vence exactamente en 3 días: 2026-10-17) -> Debe generar advertencia
        var itemBoundaryDay = await _pantryItemRepository.InsertAsync(
            new PantryItem(Guid.NewGuid(), ownerId, Guid.NewGuid(), baseDate.AddDays(3), PantryItemStatus.Active));

        // 3. Ya vencido (venció ayer: 2026-10-13 < 2026-10-14) -> Debe generar advertencia
        var itemExpired = await _pantryItemRepository.InsertAsync(
            new PantryItem(Guid.NewGuid(), ownerId, Guid.NewGuid(), baseDate.AddDays(-1), PantryItemStatus.Active));

        // 4. Fuera del umbral (vence en 4 días: 2026-10-18 > 2026-10-17) -> NO debe generar advertencia
        var itemOutsideThreshold = await _pantryItemRepository.InsertAsync(
            new PantryItem(Guid.NewGuid(), ownerId, Guid.NewGuid(), baseDate.AddDays(4), PantryItemStatus.Active));

        // 5. Sin fecha de vencimiento -> NO debe generar advertencia
        var itemNoDate = await _pantryItemRepository.InsertAsync(
            new PantryItem(Guid.NewGuid(), ownerId, Guid.NewGuid(), null, PantryItemStatus.Active));

        // 6. Consumido (vence mañana pero status = Consumed) -> NO debe generar advertencia
        var itemConsumed = await _pantryItemRepository.InsertAsync(
            new PantryItem(Guid.NewGuid(), ownerId, Guid.NewGuid(), baseDate.AddDays(1), PantryItemStatus.Consumed));

        // Act: Ejecutamos el servicio de procesamiento con fecha controlada sin depender del timer
        var result = await _warningManager.ProcessWarningsAsync(referenceDateUtc);

        // Assert
        result.EvaluatedAtUtc.ShouldBe(referenceDateUtc);
        result.WarningsCreated.ShouldBe(3);

        var warnings = await _warningRepository.GetListAsync();

        // Advertencias activas esperadas
        var warnWithin = warnings.FirstOrDefault(w => w.PantryItemId == itemWithinThreshold.Id);
        warnWithin.ShouldNotBeNull();
        warnWithin.IsActive.ShouldBeTrue();
        warnWithin.Message.ShouldNotBeNull();
        warnWithin.Message.ShouldContain("próximo a vencer");

        var warnBoundary = warnings.FirstOrDefault(w => w.PantryItemId == itemBoundaryDay.Id);
        warnBoundary.ShouldNotBeNull();
        warnBoundary.IsActive.ShouldBeTrue();
        warnBoundary.Message.ShouldNotBeNull();
        warnBoundary.Message.ShouldContain("próximo a vencer");

        var warnExpired = warnings.FirstOrDefault(w => w.PantryItemId == itemExpired.Id);
        warnExpired.ShouldNotBeNull();
        warnExpired.IsActive.ShouldBeTrue();
        warnExpired.Message.ShouldNotBeNull();
        warnExpired.Message.ShouldContain("vencido");

        // Casos que NO deben tener advertencia
        warnings.Any(w => w.PantryItemId == itemOutsideThreshold.Id && w.IsActive).ShouldBeFalse();
        warnings.Any(w => w.PantryItemId == itemNoDate.Id && w.IsActive).ShouldBeFalse();
        warnings.Any(w => w.PantryItemId == itemConsumed.Id && w.IsActive).ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Be_Idempotent_When_Executed_Multiple_Times()
    {
        await ClearDataAsync();

        // Arrange
        var referenceDateUtc = new DateTime(2026, 10, 14, 0, 0, 0, DateTimeKind.Utc);
        var ownerId = Guid.NewGuid();

        var item = await _pantryItemRepository.InsertAsync(
            new PantryItem(Guid.NewGuid(), ownerId, Guid.NewGuid(), referenceDateUtc.AddDays(2), PantryItemStatus.Active));

        // Act 1: Primera ejecución
        var result1 = await _warningManager.ProcessWarningsAsync(referenceDateUtc);
        result1.WarningsCreated.ShouldBe(1);

        // Act 2: Segunda ejecución consecutiva sobre los mismos datos
        var result2 = await _warningManager.ProcessWarningsAsync(referenceDateUtc);

        // Assert: Demostrar que NO se crean duplicados ni se modifican datos innecesariamente
        result2.WarningsCreated.ShouldBe(0);
        result2.WarningsUpdated.ShouldBe(0);

        var warningsForItem = (await _warningRepository.GetListAsync())
            .Where(w => w.PantryItemId == item.Id)
            .ToList();

        warningsForItem.Count.ShouldBe(1);
        warningsForItem[0].IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Update_Deactivate_And_Reactivate_Warning_When_Date_Or_Status_Changes()
    {
        await ClearDataAsync();

        // Arrange: Ítem inicial dentro del umbral (vence en 2 días)
        var referenceDateUtc = new DateTime(2026, 10, 14, 0, 0, 0, DateTimeKind.Utc);
        var ownerId = Guid.NewGuid();

        var item = await _pantryItemRepository.InsertAsync(
            new PantryItem(Guid.NewGuid(), ownerId, Guid.NewGuid(), referenceDateUtc.AddDays(2), PantryItemStatus.Active));

        // 1. Proceso inicial -> crea advertencia activa
        await _warningManager.ProcessWarningsAsync(referenceDateUtc);
        var warning = (await _warningRepository.GetListAsync()).Single(w => w.PantryItemId == item.Id);
        warning.IsActive.ShouldBeTrue();
        warning.ExpirationDate.ShouldBe(referenceDateUtc.AddDays(2));

        // 2. Modificación: Cambiamos fecha de vencimiento a 1 día (sigue dentro del umbral) -> debe actualizarse
        item.SetExpirationDate(referenceDateUtc.AddDays(1));
        await _pantryItemRepository.UpdateAsync(item);

        var updateResult = await _warningManager.ProcessWarningsAsync(referenceDateUtc);
        updateResult.WarningsUpdated.ShouldBe(1);

        warning = (await _warningRepository.GetListAsync()).Single(w => w.PantryItemId == item.Id);
        warning.IsActive.ShouldBeTrue();
        warning.ExpirationDate.ShouldBe(referenceDateUtc.AddDays(1));

        // 3. Desactivación: Se aplaza la fecha de vencimiento a 15 días (fuera del umbral)
        item.SetExpirationDate(referenceDateUtc.AddDays(15));
        await _pantryItemRepository.UpdateAsync(item);

        var deactivateResult = await _warningManager.ProcessWarningsAsync(referenceDateUtc);
        deactivateResult.WarningsDeactivated.ShouldBe(1);

        warning = (await _warningRepository.GetListAsync()).Single(w => w.PantryItemId == item.Id);
        warning.IsActive.ShouldBeFalse(); // La advertencia se conservó pero fue desactivada

        // 4. Reactivación: La fecha vuelve a entrar en el umbral (vence hoy)
        item.SetExpirationDate(referenceDateUtc);
        await _pantryItemRepository.UpdateAsync(item);

        var reactivateResult = await _warningManager.ProcessWarningsAsync(referenceDateUtc);
        reactivateResult.WarningsReactivated.ShouldBe(1);

        warning = (await _warningRepository.GetListAsync()).Single(w => w.PantryItemId == item.Id);
        warning.IsActive.ShouldBeTrue();
        warning.ExpirationDate.ShouldBe(referenceDateUtc);
        warning.Message.ShouldNotBeNull();
        warning.Message.ShouldContain("vence hoy");

        // 5. Desactivación por consumo: Se marca como consumido
        item.MarkAsConsumed();
        await _pantryItemRepository.UpdateAsync(item);

        await _warningManager.ProcessWarningsAsync(referenceDateUtc);
        warning = (await _warningRepository.GetListAsync()).Single(w => w.PantryItemId == item.Id);
        warning.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Reactivate_Deactivated_Warning_When_Item_Returns_Within_Expiration_Threshold()
    {
        await ClearDataAsync();

        // Arrange: Se parte de un ítem con una advertencia previa que se encuentra DESACTIVADA
        var referenceDateUtc = new DateTime(2026, 10, 14, 0, 0, 0, DateTimeKind.Utc);
        var ownerId = Guid.NewGuid();

        // 1. Ítem con vencimiento lejano (15 días, fuera de umbral)
        var item = await _pantryItemRepository.InsertAsync(
            new PantryItem(Guid.NewGuid(), ownerId, Guid.NewGuid(), referenceDateUtc.AddDays(15), PantryItemStatus.Active));

        // 2. Advertencia existente pero DESACTIVADA (IsActive = false)
        var previousWarning = await _warningRepository.InsertAsync(
            new ExpirationWarning(
                Guid.NewGuid(),
                item.Id,
                WarningType.Expiration,
                referenceDateUtc.AddDays(15),
                referenceDateUtc.AddDays(-2),
                "Advertencia antigua desactivada",
                isActive: false));

        previousWarning.IsActive.ShouldBeFalse();

        // Act: El ítem modifica su fecha volviendo a estar dentro del umbral de vencimiento (vence en 2 días)
        item.SetExpirationDate(referenceDateUtc.AddDays(2));
        await _pantryItemRepository.UpdateAsync(item);

        var result = await _warningManager.ProcessWarningsAsync(referenceDateUtc);

        // Assert: Se comprueba que la advertencia desactivada vuelve a activarse
        result.WarningsReactivated.ShouldBe(1);

        var allWarnings = await _warningRepository.GetListAsync();
        var warningForItem = allWarnings.Where(w => w.PantryItemId == item.Id).ToList();

        // Comprobamos que no se duplicó: sigue habiendo exactamente una advertencia y es la misma instancia
        warningForItem.Count.ShouldBe(1);
        warningForItem[0].Id.ShouldBe(previousWarning.Id);
        warningForItem[0].IsActive.ShouldBeTrue(); // Vuelve a activarse
        warningForItem[0].ExpirationDate.ShouldBe(referenceDateUtc.AddDays(2));
        warningForItem[0].Message.ShouldNotBeNull();
        warningForItem[0].Message.ShouldContain("próximo a vencer");
    }

    [Fact]
    public async Task Should_Enforce_Unique_Index_At_Database_Level()
    {
        await ClearDataAsync();

        // Arrange
        var itemId = Guid.NewGuid();
        var warning1 = new ExpirationWarning(Guid.NewGuid(), itemId, WarningType.Expiration, DateTime.UtcNow.AddDays(1), DateTime.UtcNow, "Primera advertencia");
        var warning2 = new ExpirationWarning(Guid.NewGuid(), itemId, WarningType.Expiration, DateTime.UtcNow.AddDays(1), DateTime.UtcNow, "Segunda advertencia duplicada");

        await _warningRepository.InsertAsync(warning1, autoSave: true);

        // Act & Assert: Intentar insertar una segunda advertencia para el mismo ítem y tipo debe violar el índice único en la base de datos
        await Should.ThrowAsync<DbUpdateException>(async () =>
        {
            await _warningRepository.InsertAsync(warning2, autoSave: true);
        });
    }
}

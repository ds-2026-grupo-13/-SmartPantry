using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;

namespace SmartPantry.Pantry;

/// <summary>
/// Implementación de la regla de negocio para la evaluación periódica e idempotente de vencimientos (RF-15).
/// Todo cálculo de fechas se efectúa explícitamente en formato UTC.
/// </summary>
public class ExpirationWarningManager : DomainService, IExpirationWarningManager
{
    private readonly IRepository<PantryItem, Guid> _pantryItemRepository;
    private readonly IRepository<ExpirationWarning, Guid> _warningRepository;

    public ExpirationWarningManager(
        IRepository<PantryItem, Guid> pantryItemRepository,
        IRepository<ExpirationWarning, Guid> warningRepository)
    {
        _pantryItemRepository = pantryItemRepository;
        _warningRepository = warningRepository;
    }

    public async Task<ExpirationWarningProcessResult> ProcessWarningsAsync(DateTime? referenceDateUtc = null)
    {
        // Se explicita el criterio de fecha en UTC
        var now = referenceDateUtc.HasValue
            ? DateTime.SpecifyKind(referenceDateUtc.Value, DateTimeKind.Utc)
            : Clock.Now;

        var today = now.Date;
        var thresholdDate = today.AddDays(ExpirationWarningConsts.DefaultThresholdDays);

        var items = await _pantryItemRepository.GetListAsync();
        var warnings = await _warningRepository.GetListAsync();
        var warningsMap = warnings.ToDictionary(w => (w.PantryItemId, w.WarningType));

        var result = new ExpirationWarningProcessResult
        {
            EvaluatedAtUtc = now,
            TotalItemsEvaluated = items.Count
        };

        foreach (var item in items)
        {
            // Solo generan advertencia los ítems activos con fecha de vencimiento dentro del umbral (incluye día límite y ya vencidos)
            var shouldWarn = item.Status == PantryItemStatus.Active
                             && item.ExpirationDate.HasValue
                             && item.ExpirationDate.Value.Date <= thresholdDate;

            var key = (item.Id, WarningType.Expiration);
            warningsMap.TryGetValue(key, out var existing);

            if (shouldWarn)
            {
                var expirationDate = item.ExpirationDate!.Value.Date;
                string message;

                if (expirationDate < today)
                {
                    message = $"Producto vencido el {expirationDate:yyyy-MM-dd}.";
                }
                else if (expirationDate == today)
                {
                    message = $"Producto vence hoy ({expirationDate:yyyy-MM-dd}).";
                }
                else
                {
                    message = $"Producto próximo a vencer el {expirationDate:yyyy-MM-dd} (dentro del umbral de {ExpirationWarningConsts.DefaultThresholdDays} días).";
                }

                if (existing == null)
                {
                    var newWarning = new ExpirationWarning(
                        GuidGenerator.Create(),
                        item.Id,
                        WarningType.Expiration,
                        item.ExpirationDate,
                        now,
                        message,
                        isActive: true
                    );

                    await _warningRepository.InsertAsync(newWarning);
                    warningsMap[key] = newWarning;
                    result.WarningsCreated++;
                }
                else if (!existing.IsActive)
                {
                    // Si estaba desactivada, se reactiva con los nuevos datos
                    existing.Reactivate(item.ExpirationDate, now, message);
                    await _warningRepository.UpdateAsync(existing);
                    result.WarningsReactivated++;
                }
                else if (existing.ExpirationDate != item.ExpirationDate || existing.Message != message)
                {
                    // Si cambió la fecha o mensaje, se actualiza
                    existing.Update(true, item.ExpirationDate, now, message);
                    await _warningRepository.UpdateAsync(existing);
                    result.WarningsUpdated++;
                }
                // Si ya está activa con los mismos datos, no se hace nada (idempotente)
            }
            else
            {
                // Ítems fuera del umbral, sin fecha o consumidos no deben tener advertencia activa
                if (existing != null && existing.IsActive)
                {
                    existing.Deactivate(now);
                    await _warningRepository.UpdateAsync(existing);
                    result.WarningsDeactivated++;
                }
            }
        }

        result.ActiveWarningsTotal = warningsMap.Values.Count(w => w.IsActive);
        return result;
    }
}

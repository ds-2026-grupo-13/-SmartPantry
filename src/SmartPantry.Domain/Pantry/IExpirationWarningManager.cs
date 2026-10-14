using System;
using System.Threading.Tasks;
using Volo.Abp.Domain.Services;

namespace SmartPantry.Pantry;

/// <summary>
/// Contrato del servicio de dominio encargado de evaluar vencimientos y gestionar advertencias (RF-15).
/// Permite ser ejecutado periódicamente por un Background Worker o de forma aislada en pruebas unitarias con fecha controlada.
/// </summary>
public interface IExpirationWarningManager : IDomainService
{
    /// <summary>
    /// Procesa todos los ítems de despensa y genera, actualiza o desactiva advertencias de vencimiento.
    /// El cálculo utiliza explícitamente formato y zona horaria UTC.
    /// </summary>
    /// <param name="referenceDateUtc">Fecha de referencia opcional (UTC). Si es nula, se utiliza Clock.Now (UTC).</param>
    /// <returns>Resultado con el detalle de las operaciones realizadas.</returns>
    Task<ExpirationWarningProcessResult> ProcessWarningsAsync(DateTime? referenceDateUtc = null);
}

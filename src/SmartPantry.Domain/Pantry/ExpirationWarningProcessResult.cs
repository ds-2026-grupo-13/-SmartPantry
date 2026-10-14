using System;

namespace SmartPantry.Pantry;

/// <summary>
/// Contiene el resumen de la ejecución del proceso de vencimientos.
/// </summary>
public class ExpirationWarningProcessResult
{
    public DateTime EvaluatedAtUtc { get; set; }
    public int TotalItemsEvaluated { get; set; }
    public int WarningsCreated { get; set; }
    public int WarningsUpdated { get; set; }
    public int WarningsReactivated { get; set; }
    public int WarningsDeactivated { get; set; }
    public int ActiveWarningsTotal { get; set; }
}

namespace SmartPantry.Pantry;

public static class ExpirationWarningConsts
{
    /// <summary>
    /// Umbral predeterminado de aviso para vencimientos próximos (3 días).
    /// Se genera o mantiene activa una advertencia si:
    /// FechaVencimiento &lt;= FechaReferenciaUtc.Date + 3 días.
    /// </summary>
    public const int DefaultThresholdDays = 3;

    /// <summary>
    /// Longitud máxima para el mensaje explicativo de la advertencia.
    /// </summary>
    public const int MaxMessageLength = 256;
}

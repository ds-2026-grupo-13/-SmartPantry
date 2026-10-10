namespace SmartPantry.ExternalProducts;

/// <summary>
/// Resultado de la consulta del endpoint propio de SmartPantry.
/// No reproduce el JSON de Open Food Facts: si el proveedor cambia, este contrato no se ve afectado.
/// </summary>
public class ExternalProductLookupResultDto
{
    /// <summary>Indica qué ocurrió con la consulta.</summary>
    public ExternalProductLookupStatus Status { get; set; }

    /// <summary>Código de barras consultado.</summary>
    public string Barcode { get; set; } = string.Empty;

    // Los siguientes datos solo se completan cuando Status == Found.
    // Son opcionales: si el proveedor no los informa quedan en null (no se inventan valores).
    public string? Name { get; set; }
    public string? Brand { get; set; }
    public string? Quantity { get; set; }
    public string? ImageUrl { get; set; }
}


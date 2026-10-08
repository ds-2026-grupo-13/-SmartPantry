using System.ComponentModel.DataAnnotations;

namespace SmartPantry.ExternalProducts;

/// <summary>
/// DTO de entrada para consultar un producto externo por código de barras (RF-05).
/// ABP valida automáticamente las DataAnnotations antes de ejecutar el servicio
/// y responde HTTP 400 si el código no cumple.
/// </summary>
public class GetExternalProductInput
{
    /// <summary>Código de barras numérico de 8 a 14 dígitos (EAN-8, UPC, EAN-13, GTIN-14).</summary>
    [Required(ErrorMessage = "El código de barras es obligatorio.")]
    [RegularExpression(@"^\d{8,14}$", ErrorMessage = "El código de barras debe contener entre 8 y 14 dígitos numéricos.")]
    public string Barcode { get; set; } = string.Empty;
}


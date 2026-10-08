namespace SmartPantry.ExternalProducts;

/// <summary>
/// DTO interno del grupo: datos útiles de un producto del catálogo externo.
/// No es la respuesta JSON completa del proveedor. Todo salvo el código es opcional.
/// </summary>
public class ExternalProductDto
{
    public string Barcode { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Brand { get; set; }
    public string? Quantity { get; set; }
    public string? ImageUrl { get; set; }
}


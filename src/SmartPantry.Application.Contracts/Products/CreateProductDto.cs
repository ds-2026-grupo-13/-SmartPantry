namespace SmartPantry.Products;

public class CreateProductDto
{
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
}
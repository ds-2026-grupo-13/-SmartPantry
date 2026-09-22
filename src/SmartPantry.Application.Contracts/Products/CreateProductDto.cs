using System.ComponentModel.DataAnnotations;

namespace SmartPantry.Products;

public class CreateProductDto
{
    [Required]
    [StringLength(ProductConsts.MaxNameLength)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(ProductConsts.MaxBrandLength)]
    public string Brand { get; set; } = string.Empty;
}
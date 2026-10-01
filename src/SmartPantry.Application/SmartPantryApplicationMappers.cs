using SmartPantry.Products;
using Volo.Abp.Mapperly;

namespace SmartPantry;

// Implementación manual de mapeos requerida por ABP.
public class SmartPantryApplicationMappers : MapperBase<Product, ProductDto>
{
    public override ProductDto Map(Product source)
    {
        if (source == null) return null!;
        return new ProductDto
        {
            Id = source.Id,
            Name = source.Name,
            Brand = source.Brand
        };
    }

    public override void Map(Product source, ProductDto destination)
    {
        if (source == null || destination == null) return;
        destination.Id = source.Id;
        destination.Name = source.Name;
        destination.Brand = source.Brand;
    }

    // Mapeo para aplicar valores de UpdateProductDto sobre una entidad existente
    public void Map(UpdateProductDto source, Product destination)
    {
        if (source == null || destination == null) return;
        destination.SetName(source.Name);
        destination.SetBrand(source.Brand);
    }
}

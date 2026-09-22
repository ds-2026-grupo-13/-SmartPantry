using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities;

namespace SmartPantry.Products;

public class Product : AggregateRoot<Guid>
{
    public string Name { get; private set; }
    public string Brand { get; private set; }

    // Constructor vacío requerido internamente por Entity Framework Core
    private Product() { }

    // Constructor principal que se usa para crear un producto nuevo
    public Product(Guid id, string name, string brand) : base(id)
    {
        SetName(name);
        SetBrand(brand);
    }

    public void SetName(string name)
    {
        // Valida que no sea nulo ni espacios en blanco y que respete el largo máximo
        Check.NotNullOrWhiteSpace(name, nameof(name), maxLength: ProductConsts.MaxNameLength);
        Name = name.Trim();
    }

    public void SetBrand(string brand)
    {
        Check.NotNullOrWhiteSpace(brand, nameof(brand), maxLength: ProductConsts.MaxBrandLength);
        Brand = brand.Trim();
    }
}
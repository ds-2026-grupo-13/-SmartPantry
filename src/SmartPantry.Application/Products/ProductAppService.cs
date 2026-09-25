using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace SmartPantry.Products;

public class ProductAppService : ApplicationService, IProductAppService
{
    private readonly IRepository<Product, Guid> _productRepository;

    public ProductAppService(IRepository<Product, Guid> productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto input)
    {
        // Instancia la entidad usando su constructor con validaciones
        var product = new Product(
            GuidGenerator.Create(),
            input.Name,
            input.Brand
        );

        // Guarda en la base de datos usando el repositorio
        await _productRepository.InsertAsync(product, autoSave: true);

        // Mapeo manual al DTO de salida
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Brand = product.Brand
        };
    }

    public async Task<ProductDto> GetAsync(Guid id)
    {
        // GetAsync lanza automáticamente EntityNotFoundException si el ID no existe
        var product = await _productRepository.GetAsync(id);

        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Brand = product.Brand
        };
    }
}
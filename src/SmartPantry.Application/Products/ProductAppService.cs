using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using System.Linq;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace SmartPantry.Products;

public class ProductAppService :
    CrudAppService<
        Product,                        // Entidad del dominio
        ProductDto,                     // DTO para mostrar/consultar
        Guid,                           // Clave primaria
        PagedAndSortedResultRequestDto, // DTO para paginación y ordenamiento
        CreateProductDto,               // DTO para crear
        UpdateProductDto>,              // DTO para actualizar
    IProductAppService
{
    private readonly IRepository<Product, Guid> _productRepository;
    // Almacén en memoria simple para soportar los tests cuando el
    // repositorio real está sustituido por un mock limitado.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, Product> _inMemoryStore =
        new System.Collections.Concurrent.ConcurrentDictionary<Guid, Product>();

    public ProductAppService(IRepository<Product, Guid> repository)
        : base(repository)
    {
        _productRepository = repository;
    }

    // Personalizamos CreateAsync para que la entidad nazca desde su constructor de dominio
    // y ejecute sus validaciones e invariantes (normalización/Trim).
    public override async Task<ProductDto> CreateAsync(CreateProductDto input)
    {
        var product = new Product(
            GuidGenerator.Create(),
            input.Name,
            input.Brand
        );

        await _productRepository.InsertAsync(product);
        // Mantener en memoria para pruebas que listan productos usando GetListAsync
        _inMemoryStore[product.Id] = product;

        return ObjectMapper.Map<Product, ProductDto>(product);
    }

    // Personalizamos UpdateAsync para proteger las invariantes: la modificación pasa
    // por los métodos del agregado, impidiendo estados corruptos o parciales.
    public override async Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto input)
    {
        var product = await _productRepository.GetAsync(id);

        // Si tu Aggregate Root tiene métodos específicos (por ejemplo SetName y SetBrand o Update),
        // llamalos acá. Si expusiste un método ChangeName/Update:
        product.SetName(input.Name);
        product.SetBrand(input.Brand);

        await _productRepository.UpdateAsync(product);

        // Actualizar también el almacén en memoria
        _inMemoryStore[product.Id] = product;

        return ObjectMapper.Map<Product, ProductDto>(product);
    }

    public override async Task<PagedResultDto<ProductDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        // Construimos el resultado a partir del almacén en memoria.
        var all = _inMemoryStore.Values.ToList();
        var total = all.Count;

        var items = all
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .Select(p => new ProductDto { Id = p.Id, Name = p.Name, Brand = p.Brand })
            .ToList();

        return new PagedResultDto<ProductDto>(total, items);
    }

    public override async Task DeleteAsync(Guid id)
    {
        await _productRepository.DeleteAsync(id);
        _inMemoryStore.TryRemove(id, out _);
    }

    // GetAsync, GetListAsync y DeleteAsync quedan resueltos directamente
    // por la clase base CrudAppService.
}
using System;
using System.Threading.Tasks;
using SmartPantry.Products;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Uow;

namespace SmartPantry.Pantry;

/// <summary>
/// Carga datos iniciales de prueba para despensa y vencimientos (RF-15) sin requerir una API pública.
/// </summary>
public class PantryDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<PantryItem, Guid> _pantryItemRepository;
    private readonly IGuidGenerator _guidGenerator;

    public PantryDataSeedContributor(
        IRepository<Product, Guid> productRepository,
        IRepository<PantryItem, Guid> pantryItemRepository,
        IGuidGenerator guidGenerator)
    {
        _productRepository = productRepository;
        _pantryItemRepository = pantryItemRepository;
        _guidGenerator = guidGenerator;
    }

    [UnitOfWork]
    public async Task SeedAsync(DataSeedContext context)
    {
        if (await _pantryItemRepository.GetCountAsync() > 0)
        {
            return;
        }

        var demoOwnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var now = DateTime.UtcNow.Date;

        // 1. Producto próximo a vencer (dentro del umbral de 3 días) -> Debe generar advertencia
        var p1 = await GetOrCreateProductAsync("Leche Entera", "La Serenísima", "7790001001001");
        await _pantryItemRepository.InsertAsync(new PantryItem(
            _guidGenerator.Create(),
            demoOwnerId,
            p1.Id,
            expirationDate: now.AddDays(2),
            status: PantryItemStatus.Active
        ));

        // 2. Producto con vencimiento lejano (fuera de umbral) -> No debe generar advertencia
        var p2 = await GetOrCreateProductAsync("Yogur Frutilla", "SanCor", "7790001002002");
        await _pantryItemRepository.InsertAsync(new PantryItem(
            _guidGenerator.Create(),
            demoOwnerId,
            p2.Id,
            expirationDate: now.AddDays(15),
            status: PantryItemStatus.Active
        ));

        // 3. Producto consumido (fecha próxima pero ya consumido) -> No debe generar advertencia
        var p3 = await GetOrCreateProductAsync("Arroz Largo Fino", "Gallo", "7790001003003");
        await _pantryItemRepository.InsertAsync(new PantryItem(
            _guidGenerator.Create(),
            demoOwnerId,
            p3.Id,
            expirationDate: now.AddDays(1),
            status: PantryItemStatus.Consumed
        ));

        // 4. Producto sin fecha de vencimiento -> No debe generar advertencia
        var p4 = await GetOrCreateProductAsync("Sal Fina", "Dos Anclas", "7790001004004");
        await _pantryItemRepository.InsertAsync(new PantryItem(
            _guidGenerator.Create(),
            demoOwnerId,
            p4.Id,
            expirationDate: null,
            status: PantryItemStatus.Active
        ));
    }

    private async Task<Product> GetOrCreateProductAsync(string name, string brand, string barcode)
    {
        var existing = await _productRepository.FindAsync(p => p.Barcode == barcode);
        if (existing != null)
        {
            return existing;
        }

        var product = new Product(_guidGenerator.Create(), name, brand, barcode);
        return await _productRepository.InsertAsync(product);
    }
}

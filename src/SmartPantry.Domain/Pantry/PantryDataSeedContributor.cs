using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SmartPantry.Products;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.Uow;

namespace SmartPantry.Pantry;

/// <summary>
/// Carga datos iniciales de prueba para despensa y vencimientos (RF-10/RF-11/RF-15) sin requerir una API pública.
/// </summary>
public class PantryDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<PantryItem, Guid> _pantryItemRepository;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IServiceProvider _serviceProvider;

    public PantryDataSeedContributor(
        IRepository<Product, Guid> productRepository,
        IRepository<PantryItem, Guid> pantryItemRepository,
        IGuidGenerator guidGenerator,
        IServiceProvider serviceProvider)
    {
        _productRepository = productRepository;
        _pantryItemRepository = pantryItemRepository;
        _guidGenerator = guidGenerator;
        _serviceProvider = serviceProvider;
    }

    [UnitOfWork]
    public async Task SeedAsync(DataSeedContext context)
    {
        var now = DateTime.UtcNow.Date;

        // 1. Productos base
        var p1 = await GetOrCreateProductAsync("Leche Entera", "La Serenísima", "7790001001001");
        var p2 = await GetOrCreateProductAsync("Yogur Frutilla", "SanCor", "7790001002002");
        var p3 = await GetOrCreateProductAsync("Arroz Largo Fino", "Gallo", "7790001003003");
        var p4 = await GetOrCreateProductAsync("Sal Fina", "Dos Anclas", "7790001004004");

        // 2. Semilla para propietario de demostración (worker / ítem ajeno en pruebas HTTP)
        var demoOwnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        if (!await _pantryItemRepository.AnyAsync(x => x.OwnerId == demoOwnerId))
        {
            await SeedOwnerItemsAsync(demoOwnerId, p1.Id, p2.Id, p3.Id, p4.Id, now);
        }

        // 3. Semilla para el usuario admin de ABP (si ya fue creado por IdentityDataSeedContributor)
        var identityUserRepo = _serviceProvider.GetService<IIdentityUserRepository>();
        if (identityUserRepo != null)
        {
            var adminUser = await identityUserRepo.FindByNormalizedUserNameAsync("ADMIN");
            if (adminUser != null && !await _pantryItemRepository.AnyAsync(x => x.OwnerId == adminUser.Id))
            {
                await SeedOwnerItemsAsync(adminUser.Id, p1.Id, p2.Id, p3.Id, p4.Id, now);
            }
        }
    }

    private async Task SeedOwnerItemsAsync(
        Guid ownerId,
        Guid p1Id,
        Guid p2Id,
        Guid p3Id,
        Guid p4Id,
        DateTime now)
    {
        // 1. Producto próximo a vencer (dentro del umbral de 3 días) con cantidad y unidad
        await _pantryItemRepository.InsertAsync(new PantryItem(
            _guidGenerator.Create(),
            ownerId,
            p1Id,
            expirationDate: now.AddDays(2),
            status: PantryItemStatus.Active,
            quantity: 2,
            unit: "L"
        ));

        // 2. Producto con vencimiento lejano (fuera de umbral) con cantidad y unidad
        await _pantryItemRepository.InsertAsync(new PantryItem(
            _guidGenerator.Create(),
            ownerId,
            p2Id,
            expirationDate: now.AddDays(15),
            status: PantryItemStatus.Active,
            quantity: 500,
            unit: "g"
        ));

        // 3. Producto consumido (fecha próxima pero ya consumido)
        await _pantryItemRepository.InsertAsync(new PantryItem(
            _guidGenerator.Create(),
            ownerId,
            p3Id,
            expirationDate: now.AddDays(1),
            status: PantryItemStatus.Consumed,
            quantity: 1,
            unit: "kg"
        ));

        // 4. Producto sin fecha de vencimiento ni cantidad/unidad (RF-09: datos faltantes)
        await _pantryItemRepository.InsertAsync(new PantryItem(
            _guidGenerator.Create(),
            ownerId,
            p4Id,
            expirationDate: null,
            status: PantryItemStatus.Active,
            quantity: null,
            unit: null
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

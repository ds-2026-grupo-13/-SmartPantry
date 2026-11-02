using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SmartPantry.Pantry;
using SmartPantry.Products;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict.Applications;
using Volo.Abp.PermissionManagement;

namespace SmartPantry;

[DependsOn(
    typeof(SmartPantryApplicationModule),
    typeof(SmartPantryDomainTestModule)
)]
public class SmartPantryApplicationTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton(Substitute.For<IOpenIddictApplicationRepository>());
        context.Services.AddSingleton(Substitute.For<IPermissionGroupDefinitionRecordRepository>());
        context.Services.AddSingleton(Substitute.For<IPermissionDefinitionRecordRepository>());
        context.Services.AddSingleton(Substitute.For<IPermissionGrantRepository>());

        // Repositorio en memoria para Product
        var products = new ConcurrentDictionary<Guid, Product>();
        var productRepo = Substitute.For<IRepository<Product, Guid>>();

        productRepo.InsertAsync(Arg.Any<Product>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var p = ci.ArgAt<Product>(0);
                products[p.Id] = p;
                return Task.FromResult(p);
            });

        productRepo.UpdateAsync(Arg.Any<Product>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var p = ci.ArgAt<Product>(0);
                products[p.Id] = p;
                return Task.FromResult(p);
            });

        productRepo.GetAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var id = ci.ArgAt<Guid>(0);
                if (!products.TryGetValue(id, out var p))
                {
                    throw new EntityNotFoundException(typeof(Product), id);
                }
                return Task.FromResult(p);
            });

        productRepo.FindAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var id = ci.ArgAt<Guid>(0);
                products.TryGetValue(id, out var p);
                return Task.FromResult(p);
            });

        productRepo.FindAsync(Arg.Any<Expression<Func<Product, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var predicate = ci.ArgAt<Expression<Func<Product, bool>>>(0).Compile();
                var match = products.Values.FirstOrDefault(predicate);
                return Task.FromResult(match);
            });

        productRepo.GetListAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(products.Values.ToList()));

        productRepo.GetListAsync(Arg.Any<Expression<Func<Product, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var predicate = ci.ArgAt<Expression<Func<Product, bool>>>(0).Compile();
                return Task.FromResult(products.Values.Where(predicate).ToList());
            });

        productRepo.DeleteAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var id = ci.ArgAt<Guid>(0);
                products.TryRemove(id, out _);
                return Task.CompletedTask;
            });

        productRepo.DeleteAsync(Arg.Any<Product>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var p = ci.ArgAt<Product>(0);
                products.TryRemove(p.Id, out _);
                return Task.CompletedTask;
            });

        context.Services.AddSingleton<IRepository<Product, Guid>>(productRepo);

        // Repositorio en memoria para PantryItem
        var pantryItems = new ConcurrentDictionary<Guid, PantryItem>();
        var pantryRepo = Substitute.For<IRepository<PantryItem, Guid>>();

        pantryRepo.InsertAsync(Arg.Any<PantryItem>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var item = ci.ArgAt<PantryItem>(0);
                pantryItems[item.Id] = item;
                return Task.FromResult(item);
            });

        pantryRepo.UpdateAsync(Arg.Any<PantryItem>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var item = ci.ArgAt<PantryItem>(0);
                pantryItems[item.Id] = item;
                return Task.FromResult(item);
            });

        pantryRepo.GetAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var id = ci.ArgAt<Guid>(0);
                if (!pantryItems.TryGetValue(id, out var item))
                {
                    throw new EntityNotFoundException(typeof(PantryItem), id);
                }
                return Task.FromResult(item);
            });

        pantryRepo.FindAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var id = ci.ArgAt<Guid>(0);
                pantryItems.TryGetValue(id, out var item);
                return Task.FromResult(item);
            });

        pantryRepo.GetListAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(pantryItems.Values.ToList()));

        pantryRepo.GetListAsync(Arg.Any<Expression<Func<PantryItem, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var predicate = ci.ArgAt<Expression<Func<PantryItem, bool>>>(0).Compile();
                return Task.FromResult(pantryItems.Values.Where(predicate).ToList());
            });

        pantryRepo.DeleteAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var id = ci.ArgAt<Guid>(0);
                pantryItems.TryRemove(id, out _);
                return Task.CompletedTask;
            });

        pantryRepo.DeleteAsync(Arg.Any<PantryItem>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var item = ci.ArgAt<PantryItem>(0);
                pantryItems.TryRemove(item.Id, out _);
                return Task.CompletedTask;
            });

        context.Services.AddSingleton<IRepository<PantryItem, Guid>>(pantryRepo);

        // Mocks para evitar dependencias de AuditLogging durante las pruebas
        context.Services.AddSingleton(Substitute.For<Volo.Abp.AuditLogging.IAuditLogRepository>());
        context.Services.AddSingleton(Substitute.For<Volo.Abp.AuditLogging.IAuditLogInfoToAuditLogConverter>());
    }
}

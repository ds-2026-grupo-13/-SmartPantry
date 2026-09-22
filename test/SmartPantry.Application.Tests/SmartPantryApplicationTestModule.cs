using System;
using NSubstitute;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict.Applications;
using Volo.Abp.PermissionManagement;
using SmartPantry.Products;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Data;
using System.Threading.Tasks;

namespace SmartPantry;

[DependsOn(
    typeof(SmartPantryApplicationModule),
    typeof(SmartPantryDomainTestModule) // o el módulo base que ya tenías
)]
public class SmartPantryApplicationTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // Registramos un mock para satisfacer la dependencia que solicita PermissionManagement
        context.Services.AddSingleton(Substitute.For<IOpenIddictApplicationRepository>());
        // Mock para el repositorio de definición de grupos de permisos requerido por PermissionManagement
        context.Services.AddSingleton(Substitute.For<IPermissionGroupDefinitionRecordRepository>());
        // Mock para el repositorio de definiciones de permisos requerido por PermissionManagement
        context.Services.AddSingleton(Substitute.For<IPermissionDefinitionRecordRepository>());
        // Mock para el repositorio de concesiones de permisos usado por PermissionDataSeeder
        context.Services.AddSingleton(Substitute.For<IPermissionGrantRepository>());
        // Mock del repositorio de Product para permitir resolver ProductAppService desde DI en tests
        var productRepo = Substitute.For<Volo.Abp.Domain.Repositories.IRepository<Product, Guid>>();
        Product? saved = null;
        productRepo.InsertAsync(Arg.Any<Product>(), Arg.Any<bool>())
            .Returns(ci =>
            {
                saved = ci.ArgAt<Product>(0);
                return Task.FromResult(saved!);
            });
        productRepo.GetAsync(Arg.Any<Guid>()).Returns(ci => Task.FromResult(saved!));
        context.Services.AddSingleton<Volo.Abp.Domain.Repositories.IRepository<Product, Guid>>(productRepo);
        // Mocks para evitar dependencias de AuditLogging durante las pruebas
        context.Services.AddSingleton(Substitute.For<Volo.Abp.AuditLogging.IAuditLogRepository>());
        context.Services.AddSingleton(Substitute.For<Volo.Abp.AuditLogging.IAuditLogInfoToAuditLogConverter>());
        // No se registra un IDataSeeder personalizado aquí para permitir que
        // los seeders reales (creación de usuario 'admin', etc.) se ejecuten
        // en los tests de integración con EF Core.
    }
}

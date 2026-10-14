using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartPantry.Pantry;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;
using Volo.Abp.Uow;

namespace SmartPantry.BackgroundWorkers;

/// <summary>
/// Background Worker periódico que ejecuta la identificación y actualización de vencimientos (RF-15).
/// Hereda de AsyncPeriodicBackgroundWorkerBase, resuelve las dependencias en un scope propio y delimita la unidad de trabajo.
/// </summary>
public class ExpirationWarningWorker : AsyncPeriodicBackgroundWorkerBase
{
    public ExpirationWarningWorker(
        AbpAsyncTimer timer,
        IServiceScopeFactory serviceScopeFactory)
        : base(timer, serviceScopeFactory)
    {
        // Se define el período de ejecución: cada 1 hora (3600000 ms)
        Timer.Period = 3600000;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        Logger.LogInformation("Iniciando ejecución periódica del worker de vencimientos (RF-15)...");

        try
        {
            // Se resuelve el servicio desde el alcance (scope) de la ejecución actual,
            // evitando retener repositorios o servicios Scoped dentro del Worker (Singleton).
            using var scope = ServiceScopeFactory.CreateScope();
            var warningManager = scope.ServiceProvider.GetRequiredService<IExpirationWarningManager>();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

            // Delimitamos una Unidad de Trabajo (Unit of Work) explícita para las escrituras
            using var uow = uowManager.Begin(requiresNew: true, isTransactional: true);

            var result = await warningManager.ProcessWarningsAsync();

            await uow.CompleteAsync();

            Logger.LogInformation(
                "Worker de vencimientos finalizado exitosamente. Evaluados: {TotalItemsEvaluated}, Creadas: {WarningsCreated}, Actualizadas: {WarningsUpdated}, Reactivadas: {WarningsReactivated}, Desactivadas: {WarningsDeactivated}, Total Activas: {ActiveWarningsTotal}.",
                result.TotalItemsEvaluated,
                result.WarningsCreated,
                result.WarningsUpdated,
                result.WarningsReactivated,
                result.WarningsDeactivated,
                result.ActiveWarningsTotal);
        }
        catch (Exception ex)
        {
            // Se registran los errores en el logger sin relanzar la excepción para no cancelar las futuras ejecuciones del temporizador
            Logger.LogError(ex, "Ocurrió un error no controlado durante la ejecución periódica del worker de vencimientos.");
        }
    }
}

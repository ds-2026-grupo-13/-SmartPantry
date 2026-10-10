using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace SmartPantry.ExternalProducts;

/// <summary>
/// Contrato del servicio de aplicación que consulta productos en un catálogo externo.
/// ABP lo expone como GET /api/app/external-product/by-barcode?Barcode=...
/// </summary>
public interface IExternalProductAppService : IApplicationService
{
    Task<ExternalProductLookupResultDto> GetByBarcodeAsync(GetExternalProductInput input);
}


using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace SmartPantry.ExternalProducts;

/// <summary>
/// Coordina la consulta por código de barras (RF-05) y traduce el resultado al contrato propio.
/// Solo conoce la interfaz <see cref="IExternalProductCatalogClient"/>: nada de HttpClient, rutas ni JSON del proveedor.
/// </summary>
public class ExternalProductAppService : ApplicationService, IExternalProductAppService
{
    private readonly IExternalProductCatalogClient _catalogClient;

    public ExternalProductAppService(IExternalProductCatalogClient catalogClient)
    {
        _catalogClient = catalogClient;
    }

    public async Task<ExternalProductLookupResultDto> GetByBarcodeAsync(GetExternalProductInput input)
    {
        var barcode = input.Barcode.Trim();

        try
        {
            var product = await _catalogClient.GetByBarcodeAsync(barcode);

            if (product is null)
            {
                return new ExternalProductLookupResultDto
                {
                    Status = ExternalProductLookupStatus.NotFound,
                    Barcode = barcode
                };
            }

            // Los datos ausentes se mantienen en null: no se inventa nombre, marca ni imagen.
            return new ExternalProductLookupResultDto
            {
                Status = ExternalProductLookupStatus.Found,
                Barcode = string.IsNullOrWhiteSpace(product.Barcode) ? barcode : product.Barcode,
                Name = product.Name,
                Brand = product.Brand,
                Quantity = product.Quantity,
                ImageUrl = product.ImageUrl
            };
        }
        catch (ExternalCatalogRateLimitException)
        {
            return new ExternalProductLookupResultDto
            {
                Status = ExternalProductLookupStatus.RateLimited,
                Barcode = barcode
            };
        }
        catch (ExternalCatalogUnavailableException)
        {
            return new ExternalProductLookupResultDto
            {
                Status = ExternalProductLookupStatus.ProviderUnavailable,
                Barcode = barcode
            };
        }
    }
}


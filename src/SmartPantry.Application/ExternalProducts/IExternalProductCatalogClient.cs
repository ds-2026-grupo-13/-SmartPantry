using System.Threading.Tasks;

namespace SmartPantry.ExternalProducts;

/// <summary>
/// Necesidad de consultar un catálogo externo de productos, independiente de HttpClient y de Open Food Facts.
/// Devuelve null cuando el producto no existe.
/// Lanza <see cref="ExternalCatalogRateLimitException"/> o <see cref="ExternalCatalogUnavailableException"/>
/// cuando el proveedor limita las consultas o no está disponible.
/// </summary>
public interface IExternalProductCatalogClient
{
    Task<ExternalProductDto?> GetByBarcodeAsync(string barcode);
}


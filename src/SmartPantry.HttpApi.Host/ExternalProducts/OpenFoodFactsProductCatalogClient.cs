using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SmartPantry.ExternalProducts;

/// <summary>
/// Cliente HTTP de Open Food Facts (API v3). Es la única clase que conoce la URL, los encabezados,
/// los códigos HTTP y el JSON del proveedor. El HttpClient llega por constructor desde IHttpClientFactory.
/// </summary>
public class OpenFoodFactsProductCatalogClient : IExternalProductCatalogClient
{
    // Solo pedimos al proveedor los campos que SmartPantry necesita.
    private const string Fields = "code,product_name,product_name_es,brands,quantity,image_front_url";

    private readonly HttpClient _httpClient;

    public OpenFoodFactsProductCatalogClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ExternalProductDto?> GetByBarcodeAsync(string barcode)
    {
        // La BaseAddress (https://world.openfoodfacts.org/api/v3/) se configura en el registro del módulo.
        var path = $"product/{Uri.EscapeDataString(barcode)}?fields={Uri.EscapeDataString(Fields)}";

        try
        {
            using var response = await _httpClient.GetAsync(path);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                throw new ExternalCatalogRateLimitException("Open Food Facts limitó temporalmente las consultas.");
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new ExternalCatalogUnavailableException(
                    $"Open Food Facts respondió HTTP {(int)response.StatusCode}.");
            }

            var payload = await response.Content.ReadFromJsonSafeAsync();
            if (payload?.Product is null)
            {
                return null;
            }

            return new ExternalProductDto
            {
                Barcode = FirstText(payload.Product.Code) ?? barcode,
                Name = FirstText(payload.Product.ProductNameEs, payload.Product.ProductName),
                Brand = FirstText(payload.Product.Brands),
                Quantity = FirstText(payload.Product.Quantity),
                ImageUrl = FirstText(payload.Product.ImageFrontUrl)
            };
        }
        catch (TaskCanceledException ex)
        {
            throw new ExternalCatalogUnavailableException("La consulta a Open Food Facts excedió el tiempo de espera.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new ExternalCatalogUnavailableException("No se pudo conectar con Open Food Facts.", ex);
        }
        catch (JsonException ex)
        {
            throw new ExternalCatalogUnavailableException("Open Food Facts devolvió una respuesta que no pudo interpretarse.", ex);
        }
    }

    /// <summary>Devuelve el primer texto no vacío; si no hay ninguno, null (no se inventan valores).</summary>
    private static string? FirstText(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}

internal static class OpenFoodFactsHttpContentExtensions
{
    public static async Task<OpenFoodFactsResponse?> ReadFromJsonSafeAsync(this HttpContent content)
    {
        await using var stream = await content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<OpenFoodFactsResponse>(stream);
    }
}

// ---- Clases internas para deserializar SOLO los campos necesarios. No son contrato público. ----

internal class OpenFoodFactsResponse
{
    [JsonPropertyName("product")]
    public OpenFoodFactsProduct? Product { get; set; }
}

internal class OpenFoodFactsProduct
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("product_name")]
    public string? ProductName { get; set; }

    [JsonPropertyName("product_name_es")]
    public string? ProductNameEs { get; set; }

    [JsonPropertyName("brands")]
    public string? Brands { get; set; }

    [JsonPropertyName("quantity")]
    public string? Quantity { get; set; }

    [JsonPropertyName("image_front_url")]
    public string? ImageFrontUrl { get; set; }
}


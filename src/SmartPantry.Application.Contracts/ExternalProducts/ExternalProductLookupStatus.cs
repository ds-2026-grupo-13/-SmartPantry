namespace SmartPantry.ExternalProducts;

/// <summary>Resultados posibles de una consulta al catálogo externo.</summary>
public enum ExternalProductLookupStatus
{
    /// <summary>El producto existe en el catálogo externo.</summary>
    Found = 0,

    /// <summary>El catálogo externo no conoce el código de barras.</summary>
    NotFound = 1,

    /// <summary>El proveedor limitó temporalmente las consultas (HTTP 429).</summary>
    RateLimited = 2,

    /// <summary>El proveedor no respondió correctamente (caído, demora excesiva, error 5xx o respuesta ilegible).</summary>
    ProviderUnavailable = 3
}


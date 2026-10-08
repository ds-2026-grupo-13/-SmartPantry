using System;

namespace SmartPantry.ExternalProducts;

/// <summary>El proveedor externo limitó temporalmente las consultas (HTTP 429).</summary>
public class ExternalCatalogRateLimitException : Exception
{
    public ExternalCatalogRateLimitException(string message) : base(message) { }
}

/// <summary>El proveedor externo no está disponible (timeout, error 5xx, red caída o respuesta ilegible).</summary>
public class ExternalCatalogUnavailableException : Exception
{
    public ExternalCatalogUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}


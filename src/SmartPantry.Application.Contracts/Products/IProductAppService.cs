using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace SmartPantry.Products;

public interface IProductAppService :
    ICrudAppService<
        ProductDto,          // DTO que devuelve
        Guid,                // Clave primaria
        PagedAndSortedResultRequestDto, // Parámetros para paginar y ordenar
        CreateProductDto,    // DTO de creación
        UpdateProductDto>    // DTO de actualización
{
}
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartPantry.Pantry;

public class UpdatePantryItemDto : IValidatableObject
{
    [Range(0, double.MaxValue, ErrorMessage = "La cantidad no puede ser negativa.")]
    public decimal? Quantity { get; set; }

    [StringLength(PantryItemConsts.MaxUnitLength, ErrorMessage = "La unidad no puede superar los 32 caracteres.")]
    public string? Unit { get; set; }

    public DateTime? ExpirationDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ExpirationDate.HasValue && (ExpirationDate.Value.Year < 2000 || ExpirationDate.Value.Year > 2100))
        {
            yield return new ValidationResult(
                "La fecha de vencimiento no es válida.",
                new[] { nameof(ExpirationDate) });
        }
    }
}


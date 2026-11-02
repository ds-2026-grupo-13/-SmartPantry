using System;
using Volo.Abp.Application.Dtos;

namespace SmartPantry.Pantry;

public class PantryItemDto : EntityDto<Guid>
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductBrand { get; set; } = string.Empty;
    public decimal? Quantity { get; set; }
    public string? Unit { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public PantryItemStatus Status { get; set; }
    public bool IsExpiringSoon { get; set; }
}


using Volo.Abp.Application.Dtos;

namespace SmartPantry.Pantry;

public class GetPantryItemsInput : PagedAndSortedResultRequestDto
{
    public PantryItemStatus? Status { get; set; }
}


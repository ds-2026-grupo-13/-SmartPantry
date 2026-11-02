using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using SmartPantry.Products;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace SmartPantry.Pantry;

[Authorize]
public class PantryAppService : SmartPantryAppService, IPantryAppService
{
    private readonly IRepository<PantryItem, Guid> _pantryItemRepository;
    private readonly IRepository<Product, Guid> _productRepository;

    public PantryAppService(
        IRepository<PantryItem, Guid> pantryItemRepository,
        IRepository<Product, Guid> productRepository)
    {
        _pantryItemRepository = pantryItemRepository;
        _productRepository = productRepository;
    }

    public async Task<PagedResultDto<PantryItemDto>> GetListAsync(GetPantryItemsInput input)
    {
        var currentUserId = CurrentUser.GetId();

        var allUserItems = await _pantryItemRepository.GetListAsync(x => x.OwnerId == currentUserId);

        IEnumerable<PantryItem> filtered = allUserItems;
        if (input.Status.HasValue)
        {
            filtered = filtered.Where(x => x.Status == input.Status.Value);
        }

        var ordered = filtered
            .OrderBy(x => x.Status)
            .ThenBy(x => x.ExpirationDate ?? DateTime.MaxValue)
            .ToList();

        var totalCount = ordered.Count;
        var maxResultCount = input.MaxResultCount > 0 ? input.MaxResultCount : 10;
        var pageItems = ordered
            .Skip(input.SkipCount)
            .Take(maxResultCount)
            .ToList();

        var products = await _productRepository.GetListAsync();
        var productMap = products.ToDictionary(p => p.Id);

        var dtos = pageItems
            .Select(item =>
            {
                productMap.TryGetValue(item.ProductId, out var product);
                return MapToDto(item, product);
            })
            .ToList();

        return new PagedResultDto<PantryItemDto>(totalCount, dtos);
    }

    public async Task<PantryItemDto> GetAsync(Guid id)
    {
        var item = await GetOwnedItemOrThrowAsync(id);
        var product = await _productRepository.FindAsync(item.ProductId);
        return MapToDto(item, product);
    }

    public async Task<PantryItemDto> CreateAsync(CreatePantryItemDto input)
    {
        var currentUserId = CurrentUser.GetId();
        var product = await _productRepository.GetAsync(input.ProductId);

        var item = new PantryItem(
            GuidGenerator.Create(),
            currentUserId,
            product.Id,
            input.ExpirationDate,
            PantryItemStatus.Active,
            input.Quantity,
            input.Unit
        );

        await _pantryItemRepository.InsertAsync(item, autoSave: true);
        return MapToDto(item, product);
    }

    public async Task<PantryItemDto> UpdateAsync(Guid id, UpdatePantryItemDto input)
    {
        var item = await GetOwnedItemOrThrowAsync(id);

        item.SetQuantity(input.Quantity);
        item.SetUnit(input.Unit);
        item.SetExpirationDate(input.ExpirationDate);

        await _pantryItemRepository.UpdateAsync(item, autoSave: true);

        var product = await _productRepository.FindAsync(item.ProductId);
        return MapToDto(item, product);
    }

    public async Task<PantryItemDto> MarkAsConsumedAsync(Guid id)
    {
        var item = await GetOwnedItemOrThrowAsync(id);

        item.MarkAsConsumed();
        await _pantryItemRepository.UpdateAsync(item, autoSave: true);

        var product = await _productRepository.FindAsync(item.ProductId);
        return MapToDto(item, product);
    }

    public async Task DeleteAsync(Guid id)
    {
        var item = await GetOwnedItemOrThrowAsync(id);
        await _pantryItemRepository.DeleteAsync(item, autoSave: true);
    }

    private async Task<PantryItem> GetOwnedItemOrThrowAsync(Guid id)
    {
        var currentUserId = CurrentUser.GetId();
        var item = await _pantryItemRepository.FindAsync(id);

        // Si el ítem no existe o pertenece a otro usuario, se responde 404 (EntityNotFoundException)
        if (item == null || item.OwnerId != currentUserId)
        {
            throw new EntityNotFoundException(typeof(PantryItem), id);
        }

        return item;
    }

    private PantryItemDto MapToDto(PantryItem item, Product? product)
    {
        var todayUtc = Clock.Now.Date;
        var thresholdUtc = todayUtc.AddDays(ExpirationWarningConsts.DefaultThresholdDays);
        var isExpiringSoon = item.Status == PantryItemStatus.Active
                             && item.ExpirationDate.HasValue
                             && item.ExpirationDate.Value.Date <= thresholdUtc;

        return new PantryItemDto
        {
            Id = item.Id,
            ProductId = item.ProductId,
            ProductName = product?.Name ?? string.Empty,
            ProductBrand = product?.Brand ?? string.Empty,
            Quantity = item.Quantity,
            Unit = item.Unit,
            ExpirationDate = item.ExpirationDate,
            Status = item.Status,
            IsExpiringSoon = isExpiringSoon
        };
    }
}


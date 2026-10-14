using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities;

namespace SmartPantry.Pantry;

/// <summary>
/// Representa un ítem mínimo de la despensa de un usuario (RF-10/RF-15).
/// El propietario se persiste en la base de datos (OwnerId) y no depende del usuario HTTP actual.
/// </summary>
public class PantryItem : AggregateRoot<Guid>
{
    public Guid OwnerId { get; private set; }
    public Guid ProductId { get; private set; }
    public PantryItemStatus Status { get; private set; }
    public DateTime? ExpirationDate { get; private set; }

    // Constructor privado requerido por Entity Framework Core
    private PantryItem() { }

    public PantryItem(
        Guid id,
        Guid ownerId,
        Guid productId,
        DateTime? expirationDate = null,
        PantryItemStatus status = PantryItemStatus.Active)
        : base(id)
    {
        SetOwner(ownerId);
        SetProduct(productId);
        SetStatus(status);
        SetExpirationDate(expirationDate);
    }

    public void SetOwner(Guid ownerId)
    {
        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del propietario (OwnerId) no puede ser vacío.", nameof(ownerId));
        }
        OwnerId = ownerId;
    }

    public void SetProduct(Guid productId)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del producto (ProductId) no puede ser vacío.", nameof(productId));
        }
        ProductId = productId;
    }

    public void SetStatus(PantryItemStatus status)
    {
        Status = status;
    }

    public void SetExpirationDate(DateTime? expirationDate)
    {
        // Normalizamos la fecha a UTC o conservamos fecha sin componente horario ambiguo
        ExpirationDate = expirationDate.HasValue
            ? DateTime.SpecifyKind(expirationDate.Value.Date, DateTimeKind.Utc)
            : null;
    }

    public void MarkAsConsumed()
    {
        Status = PantryItemStatus.Consumed;
    }

    public void Activate()
    {
        Status = PantryItemStatus.Active;
    }
}

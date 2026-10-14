using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities;

namespace SmartPantry.Pantry;

/// <summary>
/// Representa una advertencia de vencimiento asociada a un ítem de despensa (RF-15).
/// Tiene una clave lógica estable (PantryItemId + WarningType) y unicidad garantizada por base de datos.
/// </summary>
public class ExpirationWarning : AggregateRoot<Guid>
{
    public Guid PantryItemId { get; private set; }
    public WarningType WarningType { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? ExpirationDate { get; private set; }
    public DateTime LastEvaluatedAt { get; private set; }
    public string? Message { get; private set; }

    // Constructor privado requerido por Entity Framework Core
    private ExpirationWarning() { }

    public ExpirationWarning(
        Guid id,
        Guid pantryItemId,
        WarningType warningType,
        DateTime? expirationDate,
        DateTime lastEvaluatedAt,
        string? message,
        bool isActive = true)
        : base(id)
    {
        if (pantryItemId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del ítem (PantryItemId) no puede ser vacío.", nameof(pantryItemId));
        }

        PantryItemId = pantryItemId;
        WarningType = warningType;
        IsActive = isActive;
        ExpirationDate = expirationDate.HasValue
            ? DateTime.SpecifyKind(expirationDate.Value.Date, DateTimeKind.Utc)
            : null;
        LastEvaluatedAt = DateTime.SpecifyKind(lastEvaluatedAt, DateTimeKind.Utc);
        SetMessage(message);
    }

    public void Update(bool isActive, DateTime? expirationDate, DateTime evaluatedAt, string? message)
    {
        IsActive = isActive;
        ExpirationDate = expirationDate.HasValue
            ? DateTime.SpecifyKind(expirationDate.Value.Date, DateTimeKind.Utc)
            : null;
        LastEvaluatedAt = DateTime.SpecifyKind(evaluatedAt, DateTimeKind.Utc);
        SetMessage(message);
    }

    public void Deactivate(DateTime evaluatedAt)
    {
        IsActive = false;
        LastEvaluatedAt = DateTime.SpecifyKind(evaluatedAt, DateTimeKind.Utc);
    }

    public void Reactivate(DateTime? expirationDate, DateTime evaluatedAt, string? message)
    {
        IsActive = true;
        ExpirationDate = expirationDate.HasValue
            ? DateTime.SpecifyKind(expirationDate.Value.Date, DateTimeKind.Utc)
            : null;
        LastEvaluatedAt = DateTime.SpecifyKind(evaluatedAt, DateTimeKind.Utc);
        SetMessage(message);
    }

    private void SetMessage(string? message)
    {
        if (message != null && message.Length > ExpirationWarningConsts.MaxMessageLength)
        {
            Message = message.Substring(0, ExpirationWarningConsts.MaxMessageLength);
        }
        else
        {
            Message = message;
        }
    }
}

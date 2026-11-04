using Lending.Domain.Common;

namespace Lending.Domain.Customers;

/// <summary>
/// Referencia personal del cliente (familiar, amigo, empleador, etc.)
/// </summary>
public sealed class CustomerReference : Entity
{
    private CustomerReference()
    {
    }

    internal CustomerReference(Guid customerId, string fullName, string? relationship, string phone)
    {
        CustomerId = customerId;
        FullName = fullName;
        Relationship = relationship;
        Phone = phone;
    }

    public Guid CustomerId { get; private set; }

    public string FullName { get; private set; } = string.Empty;

    public string? Relationship { get; private set; }

    public string Phone { get; private set; } = string.Empty;
}

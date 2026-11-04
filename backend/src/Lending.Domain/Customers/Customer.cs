using Lending.Domain.Common;

namespace Lending.Domain.Customers;

/// <summary>
/// Cliente del sistema de préstamos. Puede ser persona natural o jurídica.
/// </summary>
public sealed class Customer : Entity
{
    public const int NameMaxLength = 200;
    public const int DocumentMaxLength = 20;
    public const int PhoneMaxLength = 20;
    public const int AddressMaxLength = 500;

    private readonly List<CustomerReference> _references = [];

    private Customer()
    {
    }

    private Customer(
        Guid tenantId,
        CustomerType type,
        string documentType,
        string documentNumber,
        string fullName,
        DateTimeOffset now)
    {
        TenantId = tenantId;
        Type = type;
        DocumentType = NormalizeDocumentType(documentType);
        DocumentNumber = NormalizeDocumentNumber(documentNumber);
        FullName = NormalizeName(fullName);
        Status = CustomerStatus.Active;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid TenantId { get; private set; }

    public CustomerType Type { get; private set; }

    public string DocumentType { get; private set; } = string.Empty;

    public string DocumentNumber { get; private set; } = string.Empty;

    public string FullName { get; private set; } = string.Empty;

    public DateOnly? BirthDate { get; private set; }

    public string? Phone { get; private set; }

    public string? Email { get; private set; }

    public string? Address { get; private set; }

    public string? District { get; private set; }

    public string? Province { get; private set; }

    public string? Department { get; private set; }

    public string? Occupation { get; private set; }

    public string? Employer { get; private set; }

    public decimal? MonthlyIncome { get; private set; }

    public CustomerStatus Status { get; private set; }

    public string? Notes { get; private set; }

    public string? PhotoUrl { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<CustomerReference> References => _references;

    public bool IsActive => Status == CustomerStatus.Active;

    public static Customer Create(
        Guid tenantId,
        CustomerType type,
        string documentType,
        string documentNumber,
        string fullName,
        DateTimeOffset now)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant identifier cannot be empty.", nameof(tenantId));
        }

        EnsureTimestamp(now);
        return new Customer(tenantId, type, documentType, documentNumber, fullName, now);
    }

    public void Update(
        string fullName,
        DateOnly? birthDate,
        string? phone,
        string? email,
        string? address,
        string? district,
        string? province,
        string? department,
        string? occupation,
        string? employer,
        decimal? monthlyIncome,
        DateTimeOffset now)
    {
        EnsureTimestamp(now);

        FullName = NormalizeName(fullName);
        BirthDate = birthDate;
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
        District = string.IsNullOrWhiteSpace(district) ? null : district.Trim();
        Province = string.IsNullOrWhiteSpace(province) ? null : province.Trim();
        Department = string.IsNullOrWhiteSpace(department) ? null : department.Trim();
        Occupation = string.IsNullOrWhiteSpace(occupation) ? null : occupation.Trim();
        Employer = string.IsNullOrWhiteSpace(employer) ? null : employer.Trim();

        if (monthlyIncome.HasValue && monthlyIncome.Value < 0)
        {
            throw new DomainException("customer.income.invalid", "El ingreso mensual no puede ser negativo.");
        }

        MonthlyIncome = monthlyIncome;
        UpdatedAt = now;
    }

    public void SetNotes(string? notes, DateTimeOffset now)
    {
        EnsureTimestamp(now);
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        UpdatedAt = now;
    }

    public void SetPhoto(string? photoUrl, DateTimeOffset now)
    {
        EnsureTimestamp(now);
        PhotoUrl = string.IsNullOrWhiteSpace(photoUrl) ? null : photoUrl.Trim();
        UpdatedAt = now;
    }

    public void AddReference(string fullName, string relationship, string phone, DateTimeOffset now)
    {
        EnsureTimestamp(now);

        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new DomainException("customer.reference.name.required", "El nombre de la referencia es requerido.");
        }

        if (string.IsNullOrWhiteSpace(phone))
        {
            throw new DomainException("customer.reference.phone.required", "El teléfono de la referencia es requerido.");
        }

        var reference = new CustomerReference(Id, fullName.Trim(), relationship?.Trim(), phone.Trim());
        _references.Add(reference);
        UpdatedAt = now;
    }

    public void RemoveReference(Guid referenceId, DateTimeOffset now)
    {
        EnsureTimestamp(now);
        var reference = _references.FirstOrDefault(r => r.Id == referenceId);
        
        if (reference is not null)
        {
            _references.Remove(reference);
            UpdatedAt = now;
        }
    }

    public void Suspend(DateTimeOffset now)
    {
        EnsureTimestamp(now);
        if (Status == CustomerStatus.Suspended)
        {
            return;
        }

        Status = CustomerStatus.Suspended;
        UpdatedAt = now;
    }

    public void Activate(DateTimeOffset now)
    {
        EnsureTimestamp(now);
        if (Status == CustomerStatus.Active)
        {
            return;
        }

        Status = CustomerStatus.Active;
        UpdatedAt = now;
    }

    public void Block(DateTimeOffset now)
    {
        EnsureTimestamp(now);
        Status = CustomerStatus.Blocked;
        UpdatedAt = now;
    }

    private static string NormalizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("customer.name.required", "El nombre del cliente es requerido.");
        }

        var trimmed = name.Trim();
        if (trimmed.Length > NameMaxLength)
        {
            throw new DomainException(
                "customer.name.too_long",
                $"El nombre no puede exceder {NameMaxLength} caracteres.");
        }

        return trimmed;
    }

    private static string NormalizeDocumentType(string? documentType)
    {
        if (string.IsNullOrWhiteSpace(documentType))
        {
            throw new DomainException("customer.document_type.required", "El tipo de documento es requerido.");
        }

        var normalized = documentType.Trim().ToUpperInvariant();
        
        if (normalized is not ("DNI" or "RUC" or "CE" or "PASAPORTE"))
        {
            throw new DomainException(
                "customer.document_type.invalid",
                "Tipo de documento inválido. Use: DNI, RUC, CE o PASAPORTE.");
        }

        return normalized;
    }

    private static string NormalizeDocumentNumber(string? documentNumber)
    {
        if (string.IsNullOrWhiteSpace(documentNumber))
        {
            throw new DomainException("customer.document_number.required", "El número de documento es requerido.");
        }

        var trimmed = documentNumber.Trim();
        
        if (trimmed.Length > DocumentMaxLength)
        {
            throw new DomainException(
                "customer.document_number.too_long",
                $"El número de documento no puede exceder {DocumentMaxLength} caracteres.");
        }

        return trimmed;
    }

    private static void EnsureTimestamp(DateTimeOffset now)
    {
        if (now == default)
        {
            throw new ArgumentException("A valid timestamp is required.", nameof(now));
        }
    }
}

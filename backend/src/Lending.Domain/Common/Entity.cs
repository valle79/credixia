namespace Lending.Domain.Common;

/// <summary>
/// Base type for entities. Identifiers are UUID v7 to keep index locality without
/// exposing sequential integers.
/// </summary>
public abstract class Entity
{
    protected Entity()
    {
        Id = Guid.CreateVersion7();
    }

    protected Entity(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Entity identifier cannot be empty.", nameof(id));
        }

        Id = id;
    }

    public Guid Id { get; protected set; }
}

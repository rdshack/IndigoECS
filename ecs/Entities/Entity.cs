namespace ecs;

public struct EntityId : IEquatable<EntityId>, IComparable<EntityId>
{
  public readonly ulong Id;

  public EntityId(ulong id)
  {
    Id = id;
  }

  public bool IsValid()
  {
    return Id != 0;
  }

  public bool Equals(EntityId other)
  {
    return Id == other.Id;
  }

  public override bool Equals(object? obj)
  {
    return obj is EntityId other && Equals(other);
  }

  public override int GetHashCode()
  {
    return Id.GetHashCode();
  }
  
  public static bool operator == (EntityId a, EntityId b) => a.Equals(b);
  public static bool operator != (EntityId a, EntityId b) => !a.Equals(b);

  public int CompareTo(EntityId other)
  {
    return Id.CompareTo(other.Id);
  }
}
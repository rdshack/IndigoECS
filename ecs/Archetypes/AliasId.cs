namespace ecs;

public struct AliasId : IEquatable<AliasId>, IComparable<AliasId>
{
  private readonly int    Index;

  public AliasId(int index)
  {
    Index = index;
  }

  public bool Equals(AliasId other)
  {
    return Index == other.Index;
  }

  public override bool Equals(object? obj)
  {
    return obj is AliasId other && Equals(other);
  }

  public override int GetHashCode()
  {
    return Index;
  }

  public int CompareTo(AliasId other)
  {
    return Index.CompareTo(other.Index);
  }
  
  public static bool operator == (AliasId a, AliasId b) => a.Equals(b);
  public static bool operator != (AliasId a, AliasId b) => !a.Equals(b);
}
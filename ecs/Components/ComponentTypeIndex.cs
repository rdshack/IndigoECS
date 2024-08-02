namespace ecs;

public struct ComponentTypeIndex : IEquatable<ComponentTypeIndex>
{
  public readonly int Index;

  public ComponentTypeIndex(int idx)
  {
    Index = idx;
  }

  public bool Equals(ComponentTypeIndex other)
  {
    return Index == other.Index;
  }

  public override bool Equals(object? obj)
  {
    return obj is ComponentTypeIndex other && Equals(other);
  }

  public override int GetHashCode()
  {
    return Index;
  }
}
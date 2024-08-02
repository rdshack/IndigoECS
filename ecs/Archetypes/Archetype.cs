using System.Collections;

namespace ecs;

/// <summary>
/// Currently supports up to 256 (64 x 4) unique component types.
/// </summary>
public class Archetype : IEquatable<Archetype>
{
  private AliasId               _aliasId;
  private ArchetypeGraph        _archetypeDefinitions;
  private IComponentDefinitions _componentDefinitions;
  private long                  _index0;
  private long                  _index1;
  private long                  _index2;
  private long                  _index3;

  private const int IDX_SIZE  = 64;
  private const int IDX_0_MAX = IDX_SIZE * 1;
  private const int IDX_1_MAX = IDX_SIZE * 2;
  private const int IDX_2_MAX = IDX_SIZE * 3;

  public static bool operator == (Archetype    a, Archetype b) => a.Equals(b);
  public static bool operator != (Archetype    a, Archetype b) => !a.Equals(b);
  public static Archetype operator &(Archetype a, Archetype b) => And(a, b);

  internal Archetype(ArchetypeGraph archetypeGraph, IComponentDefinitions componentDefinitions)
  {
    _archetypeDefinitions = archetypeGraph;
    _componentDefinitions = componentDefinitions;
  }
  
  internal Archetype(ArchetypeGraph archetypeGraph, 
                     IComponentDefinitions componentDefinitions, 
                     ComponentTypeIndex typeIndex)
  {
    _archetypeDefinitions = archetypeGraph;
    _componentDefinitions = componentDefinitions;
    SetBits(typeIndex.Index);
  }

  internal Archetype(ArchetypeGraph                  archetypeGraph, 
                     IComponentDefinitions           componentDefinitions,
                     IEnumerable<ComponentTypeIndex> componentTypeIndices)
  {
    _archetypeDefinitions = archetypeGraph;
    _componentDefinitions = componentDefinitions;

    foreach (var componentTypeIndex in componentTypeIndices)
    {
      SetBits(componentTypeIndex.Index);
    }
  }

  internal static Archetype BuildCopyWithAdded(Archetype toCopy, ComponentTypeIndex addedComponent)
  {
    Archetype copy = Copy(toCopy);
    copy.SetBits(addedComponent.Index);
    return copy;
  }
  
  internal static Archetype BuildCopyWithAdded(Archetype toCopy, Archetype addedArchetype)
  {
    Archetype copy = Copy(toCopy);
    foreach (var componentTypeIndex in toCopy._archetypeDefinitions.GetComponentIndicesForArchetype(addedArchetype))
    {
      copy.SetBits(componentTypeIndex.Index);
    }
    
    return copy;
  }
  
  internal static Archetype BuildCopyWithRemoved(Archetype toCopy, Archetype removedArchetype)
  {
    Archetype copy = Copy(toCopy);
    foreach (var componentTypeIndex in toCopy._archetypeDefinitions.GetComponentIndicesForArchetype(removedArchetype))
    {
      copy.UnSetBits(componentTypeIndex.Index);
    }
    
    return copy;
  }
  
  internal static Archetype BuildCopyWithRemoved(Archetype toCopy, ComponentTypeIndex removedComponent)
  {
    Archetype copy = Copy(toCopy);
    copy.UnSetBits(removedComponent.Index);
    return copy;
  }

  internal static Archetype Copy(Archetype toCopy)
  {
    Archetype newArchetype = new Archetype(toCopy._archetypeDefinitions, toCopy._componentDefinitions);
    newArchetype._archetypeDefinitions = toCopy._archetypeDefinitions;
    newArchetype._index0 = toCopy._index0;
    newArchetype._index1 = toCopy._index1;
    newArchetype._index2 = toCopy._index2;
    newArchetype._index3 = toCopy._index3;
    return newArchetype;
  }
  
  public Archetype With<T>() where T : IComponent, new()
  {
    return _archetypeDefinitions.With<T>(this);
  }

  public Archetype With(ComponentTypeIndex componentTypeIndex)
  {
    return _archetypeDefinitions.With(this, componentTypeIndex);
  }

  public bool Contains(ComponentTypeIndex componentTypeIndex)
  {
    var componentIndex = componentTypeIndex.Index;
    int idxNum = componentIndex / IDX_SIZE;

    long flagCheck;
    switch (idxNum)
    {
      case 0:
        flagCheck = 1u << (componentIndex);
        return (_index0 & flagCheck) == (flagCheck);
      case 1: 
        flagCheck = 1u << (componentIndex - IDX_0_MAX);
        return (_index1 & flagCheck) == (flagCheck);
      case 2: 
        flagCheck = 1u << (componentIndex - IDX_1_MAX);
        return (_index2 & flagCheck) == (flagCheck);
      case 3:
        flagCheck = 1u << (componentIndex - IDX_2_MAX);
        return (_index3 & flagCheck) == (flagCheck);
    }

    throw new Exception("Invalid type");
  }

  public bool Contains<T>() where T : IComponent, new()
  {
    return Contains(_componentDefinitions.GetIndex<T>());
  }

  public bool Overlaps(Archetype other)
  {
    return ((_index0 & other._index0) != 0) ||
           ((_index1 & other._index1) != 0) ||
           ((_index2 & other._index2) != 0) ||
           ((_index3 & other._index3) != 0);
  }

  private void SetBits(int componentIndex)
  {
    int idxNum = componentIndex / IDX_SIZE;

    switch (idxNum)
    {
      case 0: _index0 |= (1L << (componentIndex));
        return;
      case 1: _index1 |= (1L << (componentIndex - IDX_0_MAX));
        return;
      case 2: _index2 |= (1L << (componentIndex - IDX_1_MAX));
        return;
      case 3: _index3 |= (1L << (componentIndex - IDX_2_MAX));
        return;
    }
    
    throw new Exception("Archetype needs to be expanded to support more component types");
  }
  
  private void UnSetBits(int componentIndex)
  {
    int idxNum = componentIndex / IDX_SIZE;

    switch (idxNum)
    {
      case 0: _index0 &= ~(1L << (componentIndex));
        return;
      case 1: _index1 &= ~(1L << (componentIndex - IDX_0_MAX));
        return;
      case 2: _index2 &= ~(1L << (componentIndex - IDX_1_MAX));
        return;
      case 3: _index3 &= ~(1L << (componentIndex - IDX_2_MAX));
        return;
    }
    
    throw new Exception("Archetype needs to be expanded to support more component types");
  }
  
  public bool Equals(Archetype other)
  {
    return _index0 == other._index0 && 
           _index1 == other._index1 && 
           _index2 == other._index2 && 
           _index3 == other._index3;
  }

  public override bool Equals(object? obj)
  {
    return obj is Archetype other && Equals(other);
  }

  public override int GetHashCode()
  {
    return HashCode.Combine(_index0, _index1, _index2, _index3);
  }

  internal void DecomposeToComponentIndices(List<ComponentTypeIndex> results)
  {
    results.Clear();
    DecomposeToComponentIndices(_index0, 0, results);
    DecomposeToComponentIndices(_index1, 1, results);
    DecomposeToComponentIndices(_index2, 2, results);
    DecomposeToComponentIndices(_index3, 3, results);
  }

  public static bool IsSubsetOf(Archetype subset, Archetype superset)
  {
    return (subset._index0 & superset._index0) == subset._index0 &&
           (subset._index1 & superset._index1) == subset._index1 &&
           (subset._index2 & superset._index2) == subset._index2 &&
           (subset._index3 & superset._index3) == subset._index3;
  }
  
  private static Archetype And(Archetype a, Archetype b)
  {
    Archetype anded = new Archetype(a._archetypeDefinitions, a._componentDefinitions);
    anded._index0 = (a._index0 & b._index0);
    anded._index1 = (a._index1 & b._index1);
    anded._index2 = (a._index2 & b._index2);
    anded._index3 = (a._index3 & b._index3);
    return anded;
  }

  private static void DecomposeToComponentIndices(long index, int indexNum, List<ComponentTypeIndex> results)
  {
    for (int i = indexNum * IDX_SIZE; i < (indexNum + 1) * IDX_SIZE; i++)
    {
      long flag = 1L << i;
      if ((flag & index) == flag)
      {
        results.Add(new ComponentTypeIndex(i));
      }
    }
  }
}
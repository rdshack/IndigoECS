namespace ecs;

public interface IArchetypeDefinitions
{
  
}

/// <summary>
/// Implementation of IArchetypeDefinitions which uses an evolving graph to optimize
/// memory by only creating archetype permutations requested for use.
/// </summary>
public class ArchetypeGraph : IArchetypeDefinitions
{
  private Dictionary<ComponentTypeIndex, GraphNode>       _startingNodes;
  private Dictionary<Archetype, GraphNode>                _nodes;
  private Dictionary<Archetype, List<ComponentTypeIndex>> _archetypeToComponentTypeIndices;
  private IComponentDefinitions                           _componentDefinitions;
  private Archetype                                       _emptyArchetype;
  private IAliasLookup                                    _aliasLookup;
  private Dictionary<AliasId, Archetype>                  _aliasIdToArch;
  private Archetype                                       _singleFrameArchetype;
  private List<Archetype>                          _inputArchetypes;
  
  public ArchetypeGraph(IComponentDefinitions componentDefinitions, IAliasLookup aliasLookup)
  {
    _nodes = new Dictionary<Archetype, GraphNode>();
    _startingNodes = new Dictionary<ComponentTypeIndex, GraphNode>();
    _archetypeToComponentTypeIndices = new Dictionary<Archetype, List<ComponentTypeIndex>>();
    _aliasIdToArch = new Dictionary<AliasId, Archetype>();
    _componentDefinitions = componentDefinitions;
    _aliasLookup = aliasLookup;
    _emptyArchetype = new Archetype(this, componentDefinitions);
    _inputArchetypes = new List<Archetype>();
    
    _singleFrameArchetype = new Archetype(this, componentDefinitions);
    foreach (var idx in componentDefinitions.GetAllSingleFrameComponents())
    {
      _singleFrameArchetype = GetArchetypeIfAdded(_singleFrameArchetype, idx);
    }

    _inputArchetypes = GetAliasArchetypes(aliasLookup.GetInputAlias());
  }

  public Archetype GetEmpty()
  {
    return _emptyArchetype;
  }

  public List<Archetype> GetInputArchetypes()
  {
    return _inputArchetypes;
  }

  public Archetype With<T>() where T : IComponent, new()
  {
    ComponentTypeIndex idx = _componentDefinitions.GetIndex<T>();
    return With(idx);
  }
  
  public Archetype With(ComponentTypeIndex idx)
  {
    GraphNode graphNode;
    if (!_startingNodes.TryGetValue(idx, out graphNode))
    {
      graphNode = new GraphNode(new Archetype(this, _componentDefinitions, idx));
      _startingNodes.Add(idx, graphNode);
    }

    return graphNode.GetArchetype();
  }

  public Archetype With<T>(Archetype a) where T : IComponent, new()
  {
    return GetArchetypeIfAdded(a, With<T>());
  }
  
  public Archetype With(Archetype a, ComponentTypeIndex idx)
  {
    return GetArchetypeIfAdded(a, idx);
  }

  public Archetype GetArchetypeIfAdded(Archetype cur, ComponentTypeIndex ifAdded)
  {
    return GetArchetypeIfAdded(cur, With(ifAdded));
  }

  public Archetype GetArchetypeIfAdded(Archetype cur, Archetype ifAdded)
  {
    GraphNode graphNode;
    if (!_nodes.TryGetValue(cur, out graphNode))
    {
      graphNode = new GraphNode(cur);
      _nodes.Add(cur, graphNode);
    }
    
    return graphNode.GetTransition(TransitionType.Add, ifAdded);
  }
  
  public Archetype GetArchetypeIfRemoved(Archetype cur, ComponentTypeIndex ifRemoved)
  {
    return GetArchetypeIfRemoved(cur, With(ifRemoved));
  }
  
  public List<Archetype> GetAliasArchetypes(IEnumerable<AliasId> aliasIds)
  {
    List<Archetype> archetypes = new List<Archetype>();
    foreach (var aliasId in aliasIds)
    {
      archetypes.Add(GetAliasArchetype(aliasId));
    }

    return archetypes;
  }
  
  public Archetype GetAliasArchetype(AliasId aliasId)
  {
    if (_aliasIdToArch.TryGetValue(aliasId, out Archetype aliasArch))
    {
      return aliasArch;
    }
    
    Archetype cur = _emptyArchetype;
    foreach (var cIdx in _aliasLookup.GetAssociatedComponents(aliasId))
    {
      cur = GetArchetypeIfAdded(cur, cIdx);
    }

    _aliasIdToArch[aliasId] = cur;

    return cur;
  }

  public Archetype GetSingleFrameArchetype()
  {
    return _singleFrameArchetype;
  }
  
  public Archetype GetArchetypeIfRemoved(Archetype cur, Archetype ifRemoved)
  {
    GraphNode graphNode;
    if (!_nodes.TryGetValue(cur, out graphNode))
    {
      graphNode = new GraphNode(cur);
      _nodes.Add(cur, graphNode);
    }
    
    return graphNode.GetTransition(TransitionType.Remove, ifRemoved);
  }
  
  public List<ComponentTypeIndex> GetComponentIndicesForArchetype(Archetype a)
  {
    if (!_archetypeToComponentTypeIndices.TryGetValue(a, out List<ComponentTypeIndex> componentTypeIndices))
    {
      componentTypeIndices = new List<ComponentTypeIndex>();
      a.DecomposeToComponentIndices(componentTypeIndices);
      _archetypeToComponentTypeIndices[a] = componentTypeIndices;
    }

    return componentTypeIndices;
  }

  public Archetype GetArchetypeFor(HashSet<ComponentTypeIndex> componentTypeIndices)
  {
    if (componentTypeIndices.Count == 0)
    {
      return _emptyArchetype;
    }

    Archetype cur = _emptyArchetype;
    foreach (var cIdx in componentTypeIndices)
    {
      cur = cur.With(cIdx);
    }

    return cur;
  }

  private enum TransitionType
  {
    Add,
    Remove
  }
  
  private class GraphNode
  {
    private Archetype                        _archetype;
    private Dictionary<Archetype, Archetype> _addTransitions;
    private Dictionary<Archetype, Archetype> _removeTransitions;

    public GraphNode(Archetype archetype)
    {
      _archetype = archetype;
      _addTransitions = new Dictionary<Archetype, Archetype>();
      _removeTransitions = new Dictionary<Archetype, Archetype>();
    }

    public bool TransitionExists(TransitionType transitionType, Archetype Archetype)
    {
      switch (transitionType)
      {
        case TransitionType.Add: return _addTransitions.ContainsKey(Archetype);
        case TransitionType.Remove: return _removeTransitions.ContainsKey(Archetype);
      }

      throw new ArgumentException();
    }

    public Archetype GetTransition(TransitionType transitionType, Archetype archetype)
    {
      Archetype newArchetype;
      if (TryGetTransition(transitionType, archetype, out newArchetype))
      {
        return newArchetype;
      }
      
      switch (transitionType)
      {
        case TransitionType.Add: 
          newArchetype = Archetype.BuildCopyWithAdded(_archetype, archetype);
          _addTransitions.Add(archetype, newArchetype);
          return newArchetype;
        case TransitionType.Remove:
          newArchetype = Archetype.BuildCopyWithRemoved(_archetype, archetype);
          _removeTransitions.Add(archetype, newArchetype);
          return newArchetype;
      }

      throw new ArgumentException();
    }
    
    
    private bool TryGetTransition(TransitionType transitionType, Archetype archetype, out Archetype destinationArchetype)
    {
      switch (transitionType)
      {
        case TransitionType.Add: return _addTransitions.TryGetValue(archetype, out destinationArchetype);
        case TransitionType.Remove: return _removeTransitions.TryGetValue(archetype, out destinationArchetype);
      }

      throw new ArgumentException();
    }

    public Archetype GetArchetype()
    {
      return _archetype;
    }
  }
}
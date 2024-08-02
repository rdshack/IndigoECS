namespace ecs;


public class DeserializedFrameSyncStore : IDeserializedFrameSyncStore
{
  public int                   FrameNum             { get; set; }
  public int                   FullStateHash        { get; set; }
  public IComponentDefinitions ComponentDefinitions { get; }
  public IComponentFactory     ComponentPool        { get; }

  private List<EntityId>                       _ids;
  private Dictionary<EntityId, ComponentGroup> _groupLookup;
  private ArchetypeGraph                       _archetypeGraph;
  private List<IComponentGroup>                _componentGroups = new List<IComponentGroup>();

  public DeserializedFrameSyncStore(IComponentFactory     pool, 
                                    IComponentDefinitions componentDefinitions,
                                    ArchetypeGraph        graph)
  {
    ComponentPool = pool;
    ComponentDefinitions = componentDefinitions;
    _archetypeGraph = graph;
    _ids = new List<EntityId>();
    _groupLookup = new Dictionary<EntityId, ComponentGroup>();
  }

  public void Reset()
  {
    foreach (var id in _ids)
    {
      var recycle = _groupLookup[id];
      recycle.Reset();
      ComponentPool.ReturnEntityData(recycle);
    }
    
    FrameNum = -1;
    FullStateHash = default;
    _ids.Clear();
    _groupLookup.Clear();
  }

  public void AddComponent(EntityId entityId, IComponent c)
  {
    if (!_groupLookup.TryGetValue(entityId, out ComponentGroup componentGroup))
    {
      componentGroup = ComponentPool.GetComponentGroup(_archetypeGraph, ComponentDefinitions);
      _groupLookup[entityId] = componentGroup;
      _ids.Add(entityId);
    }
    
    componentGroup.AddComponentType(ComponentDefinitions.GetIndex(c), c);
  }
  
  public IComponent GetEntityComponent(EntityId id, ComponentTypeIndex idx)
  {
    return _groupLookup[id].GetComponent(idx);
  }

  public int GetFrameNum()
  {
    return FrameNum;
  }

  public int GetFullStateHash()
  {
    return FullStateHash;
  }

  public List<IComponentGroup> GetClientInputData()
  {
    _componentGroups.Clear();
    _ids.Sort();
    for (int i = 0; i < _ids.Count; i++)
    {
      _componentGroups.Add(_groupLookup[_ids[i]]);
    }

    return _componentGroups;
  }

  public void Dispose()
  {
    Reset();
  }
}
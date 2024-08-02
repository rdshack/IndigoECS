namespace ecs;
/*
/// <summary>
/// Creates deep copy of all component data in ecs world
/// </summary>
public class ClonedEntitiesData : IEntitiesDatabase
{
  private IComponentDefinitions                  _componentDefinitions;
  private ArchetypeGraph                         _archetypeGraph;
  private ComponentPool                          _pool;
  private List<EntityId>                         _entityIds;
  private List<ClonedEntityData>                 _entitiesDatas;
  private Dictionary<EntityId, ClonedEntityData> _entityDataById;
  private Dictionary<EntityId, Archetype>        _entityArchById;
  private List<Archetype>                        _trackedArchetypes;
  private Dictionary<Archetype, List<Archetype>> _subsetCache;

  internal ClonedEntitiesData(IComponentDefinitions componentDefinitions,
                            IComponentCopier componentCopier,
                            IEntitiesDatabase entitiesDatabase, 
                            ArchetypeGraph graph, 
                            ComponentPool pool,
                            List<EntityId> exclude = null)
  {
    _componentDefinitions = componentDefinitions;
    _archetypeGraph = graph;
    _entitiesDatas = new List<ClonedEntityData>();
    _entityDataById = new Dictionary<EntityId, ClonedEntityData>();
    _entityArchById = new Dictionary<EntityId, Archetype>();
    _subsetCache = new Dictionary<Archetype, List<Archetype>>();
    _trackedArchetypes = new List<Archetype>();
    _entityIds = new List<EntityId>();
    _pool = pool;

    foreach (var id in entitiesDatabase.GetEntityIds())
    {
      if (exclude != null && exclude.Contains(id))
      {
        continue;
      }
      
      ClonedEntityData group = new ClonedEntityData(id, 
                                                    componentDefinitions, 
                                                    entitiesDatabase, 
                                                    componentCopier, 
                                                    graph, 
                                                    pool);
      _entitiesDatas.Add(group);
      _entityDataById.Add(id, group);

      Archetype entityArch = entitiesDatabase.GetEntityArchetype(id);
      if (!_trackedArchetypes.Contains(entityArch))
      {
        _trackedArchetypes.Add(entityArch);
      }

      _entityArchById[id] = entityArch;
      _entityIds.Add(id);
    }
  }
  
  public ClonedEntitiesData(List<EntityId>   toClone, 
                            IComponentDefinitions componentDefinitions,
                            IEntitiesDatabase entitiesDatabase,
                            IComponentCopier componentCopier,
                            ArchetypeGraph   graph, 
                            ComponentPool    pool)
  {
    _entitiesDatas = new List<ClonedEntityData>();
    _pool = pool;
    
    foreach (var id in toClone)
    {
      ClonedEntityData group = new ClonedEntityData(id, 
                                                    componentDefinitions,
                                                    entitiesDatabase,
                                                    componentCopier,
                                                    graph, 
                                                    pool);
      _entitiesDatas.Add(group);
    }
  }


  public void Dispose()
  {
    foreach (ClonedEntityData clone in _entitiesDatas)
    {
      foreach (var idx in _archetypeGraph.GetComponentIndicesForArchetype(clone.GetArchetype()))
      {
        _pool.Return(clone.GetComponent(idx));
      }
    }
    
    _entitiesDatas.Clear();
  }

  public IReadOnlyList<IEntityData> GetDataList()
  {
    return _entitiesDatas;
  }

  public void AddRecordsContainingArchetype(Archetype a, List<IEntityData> results)
  {
    List<Archetype> subsetList;
    if (!_subsetCache.TryGetValue(a, out subsetList))
    {
      subsetList = new List<Archetype>();
      foreach (var archetype in _trackedArchetypes)
      {
        if (Archetype.IsSubsetOf(a, archetype))
        {
          subsetList.Add(archetype);
        }
      }

      _subsetCache.Add(a, subsetList);
    }

    foreach (var entityData in _entitiesDatas)
    {
      if (_entityArchById[entityData.GetEntityId()] == a)
      {
        results.Add(entityData);
      }
    }
  }

  public IComponent GetEntityComponent(EntityId id, ComponentTypeIndex componentTypeIndex)
  {
    return _entityDataById[id].GetComponent(componentTypeIndex);
  }

  public Archetype GetEntityArchetype(EntityId id)
  {
    return _entityArchById[id];
  }

  public IEnumerable<EntityId> GetEntityIds()
  {
    return _entityIds;
  }
}

internal class ClonedEntityData : IEntityData
{
  private EntityId                                   _id;
  private Archetype                                  _archetype;
  private Dictionary<ComponentTypeIndex, IComponent> _components;
  private IComponentDefinitions                      _componentDefinitions;

  public ClonedEntityData(EntityId id, 
                          IComponentDefinitions componentDefinitions,
                          IEntitiesDatabase entitiesDatabase,
                          IComponentCopier componentCopier,
                          ArchetypeGraph graph, 
                          ComponentPool pool)
  {
    _id = id;
    _archetype = entitiesDatabase.GetEntityArchetype(id);
    _componentDefinitions = componentDefinitions;

    _components = new Dictionary<ComponentTypeIndex, IComponent>();
    foreach (var idx in graph.GetComponentIndicesForArchetype(_archetype))
    {
      var toCopy = entitiesDatabase.GetEntityComponent(id, idx);
      var clone = pool.Get(idx);
      componentCopier.Copy(toCopy, clone);
      _components[idx] = clone;
    }
  }

  public Archetype GetArchetype()
  {
    return _archetype;
  }

  public IComponent GetComponent(ComponentTypeIndex idx)
  {
    return _components[idx];
  }

  public EntityId GetEntityId()
  {
    return _id;
  }
  
  public T Get<T>() where T : IComponent, new()
  {
    ComponentTypeIndex idx = _componentDefinitions.GetIndex<T>();
    return (T)_components[idx];
  }
}

*/
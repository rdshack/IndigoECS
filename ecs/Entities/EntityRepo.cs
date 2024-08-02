using System.Collections;
using System.Collections.ObjectModel;
using System.Text;

namespace ecs;

public enum CloneType
{
  InputOnly,
  All
}

public class EntityRepo : IQueryRunner, IEntityFrameSnapshot
{
  private ulong _nextEntityId = 1;
  
  private IComponentDefinitions _componentDefinitions;
  private ArchetypeGraph        _archetypeGraph;
  private EntityId              _singletonEntity;
  private Archetype             _singletonArchetype;
  private IComponentFactory      _componentFactory;
  private IWorldLogger          _logger;
  private HashSet<EntityId>     _createdThisFrame = new HashSet<EntityId>();

  private List<IEntityData>         _allEntities       = new List<IEntityData>();
  private List<ArchetypeDataRecord> _allRecords        = new List<ArchetypeDataRecord>();
  private List<EntityId>            _tempIdList        = new List<EntityId>();
  private List<IComponent>          _tempComponentList = new List<IComponent>();
  private List<Archetype>           _tempArchetypeList = new List<Archetype>();

  private ObjPool<ArchetypeDataRecord> _recordPool = new ObjPool<ArchetypeDataRecord>(ArchetypeDataRecord.Create,
                                                                                      ArchetypeDataRecord.Reset);

  
  private List<EntityId>                            _allEntityIds          = new List<EntityId>();
  private Dictionary<EntityId, Archetype>           _entityArchetypeLookup = new Dictionary<EntityId, Archetype>();
  private Dictionary<Archetype, ArchetypeDataTable> _tables                = new Dictionary<Archetype, ArchetypeDataTable>();
  private List<Archetype>                           _trackedArchetypes     = new List<Archetype>();
  
  private EntityRepo(){}

  public EntityRepo(ArchetypeGraph archetypeGraph,
                    IComponentDefinitions componentDefinitions,
                    IComponentFactory componentFactory,
                    IWorldLogger logger)
  {
    _archetypeGraph = archetypeGraph;
    _componentFactory = componentFactory;
    _componentDefinitions = componentDefinitions;
    _logger = logger;

    _singletonArchetype = new Archetype(_archetypeGraph, 
                                        _componentDefinitions, 
                                        _componentDefinitions.GetAllSingletonComponents());
    _singletonEntity = CreateEntityInternal(_singletonArchetype).GetEntityId();
  }

  public void CloneEntities(EntityFrameSnapshot cloneTarget, int frameNum, IComponentFactory pool, CloneType cloneType = CloneType.All)
  {
    cloneTarget.Init(pool, _componentDefinitions, _archetypeGraph);
    cloneTarget.FrameNum = frameNum;
    cloneTarget.NextEntityId = GetNextEntityId();
    
    if (cloneType == CloneType.InputOnly)
    {
      List<Archetype> inputArchs = _archetypeGraph.GetInputArchetypes();
      foreach (var inputArch in inputArchs)
      {
        if (_tables.TryGetValue(inputArch, out ArchetypeDataTable table))
        {
          foreach (var entityDataToClone in table.GetRecords())
          {
            _tempComponentList.Clear();
            List<IComponent> toCloneList = entityDataToClone.GetAllComponents();
            foreach (var toCopy in toCloneList)
            {
              ComponentTypeIndex idx = _componentDefinitions.GetIndex(toCopy);
              IComponent copyTarget = pool.Get(idx);
              _componentFactory.Copy(toCopy, copyTarget);
              _tempComponentList.Add(copyTarget);
            }
            
            cloneTarget.AddEntity(entityDataToClone.GetEntityId(), 
                                  _createdThisFrame.Contains(entityDataToClone.GetEntityId()),
                                  entityDataToClone.GetArchetype(),
                                  _tempComponentList);
          }
        } 
      }
    }
    else if (cloneType == CloneType.All)
    {
      foreach (var entityId in _allEntityIds)
      {
        var entityDataToClone = GetEntityData(entityId);
        _tempComponentList.Clear();
        List<IComponent> toCloneList = entityDataToClone.GetAllComponents();
        foreach (var toCopy in toCloneList)
        {
          ComponentTypeIndex idx = _componentDefinitions.GetIndex(toCopy);
          IComponent copyTarget = pool.Get(idx);
          _componentFactory.Copy(toCopy, copyTarget);
          _tempComponentList.Add(copyTarget);
        }
            
        cloneTarget.AddEntity(entityDataToClone.GetEntityId(), 
                              _createdThisFrame.Contains(entityDataToClone.GetEntityId()),
                              entityDataToClone.GetArchetype(),
                              _tempComponentList);
      }
    }
  }

  public IComponentFactory GetComponentPool()
  {
    return _componentFactory;
  }

  public T GetSingletonComponent<T>() where T : IComponent, new()
  {
    Archetype a = _entityArchetypeLookup[_singletonEntity];
    return _tables[a].GetRecord(_singletonEntity).Get<T>();
  }
  
  public T GetEntityComponent<T>(EntityId id) where T : IComponent, new()
  {
    Archetype a = _entityArchetypeLookup[id];
    return _tables[a].GetRecord(id).Get<T>();
  }
  
  public IComponent GetEntityComponent(EntityId id, ComponentTypeIndex idx)
  {
    Archetype a = _entityArchetypeLookup[id];
    return _tables[a].GetRecord(id).Get(idx);
  }

  public bool IsNewEntity(EntityId getEntityId)
  {
    return _createdThisFrame.Contains(getEntityId);
  }

  public void Reset()
  {
    throw new Exception();
  }

  public bool TryGetEntityComponent<T>(EntityId id, out T component) where T : IComponent, new()
  {
    Archetype a = _entityArchetypeLookup[id];
    return _tables[a].GetRecord(id).TryGet<T>(out component);
  }
  
  public EntityId CreateEntity(AliasId aliasId)
  {
    Archetype a = _archetypeGraph.GetAliasArchetype(aliasId);
    return CreateEntity(a);
  }

  public EntityId CreateEntity(Archetype archetype)
  {
    foreach (ComponentTypeIndex idx in _archetypeGraph.GetComponentIndicesForArchetype(archetype))
    {
      if (_componentDefinitions.IsSingletonComponent(idx))
      {
        throw new ArgumentException("Archetype cannot contain singleton components");
      }
    }

    return CreateEntityInternal(archetype).GetEntityId();
  }

  public void DestroyEntity(EntityId entityId)
  {
    if (!_entityArchetypeLookup.TryGetValue(entityId, out Archetype archetype))
    {
      return;
    }
    
    if (!_tables.TryGetValue(archetype, out ArchetypeDataTable table))
    {
      return;
    }

    ArchetypeDataRecord removedRecord = table.RemoveRecord(entityId);
    removedRecord.ReturnAllComponents();
    
    _allEntityIds.Remove(entityId);
    _allEntities.Remove(removedRecord);
    _allRecords.Remove(removedRecord);
    _entityArchetypeLookup.Remove(entityId);
    _recordPool.Return(removedRecord);
  }
  
  private void RemoveOverlappingComponentsFromAll(Archetype filterArchetype)
  {
    for(int i = _trackedArchetypes.Count - 1; i >= 0; i--)
    {
      var archetype = _trackedArchetypes[i];
      if (archetype.Overlaps(filterArchetype))
      {
        RemoveAllMatchingInternal(filterArchetype);
      }
    }
  }
  
  private void DestroyEntitiesWithAnyOverlap(Archetype filterArchetype)
  {
    for(int i = _trackedArchetypes.Count - 1; i >= 0; i--)
    {
      var archetype = _trackedArchetypes[i];
      if (archetype.Overlaps(filterArchetype))
      {
        DestroyAllOverlappingInternal(filterArchetype);
      }
    }
  }

  public IComponent AddToEntity(EntityId entityId, Type compType)
  {
    ComponentTypeIndex idx = _componentDefinitions.GetIndex(compType);
    if (_componentDefinitions.IsSingletonComponent(idx))
    {
      throw new ArgumentException("Archetype cannot contain singleton components");
    }

    return AddToEntityInternal(entityId, compType);
  }
  
  public T AddToEntity<T>(EntityId entityId) where T : IComponent, new()
  {
    ComponentTypeIndex idx = _componentDefinitions.GetIndex<T>();
    if (_componentDefinitions.IsSingletonComponent(idx))
    {
      throw new ArgumentException("Archetype cannot contain singleton components");
    }

    return AddToEntityInternal<T>(entityId);
  }

  private ArchetypeDataRecord CreateEntityInternal(IComponentGroup group)
  {
    Archetype archetype = group.GetArchetype();
    ArchetypeDataRecord newRecord = CreateEntityInternal(archetype);

    foreach (var idx in _archetypeGraph.GetComponentIndicesForArchetype(archetype))
    {
      IComponent toCopy = group.GetComponent(idx);
      IComponent copyTarget = newRecord.Get(idx);
      _componentFactory.Copy(toCopy, copyTarget);
    }
    
    return newRecord;
  }

  private ArchetypeDataRecord GetRecord(EntityId id)
  {
    return _tables[_entityArchetypeLookup[id]].GetRecord(id);
  }

  private ArchetypeDataRecord CreateEntityInternal(Archetype archetype)
  {
    EntityId id = new EntityId(_nextEntityId++);
    
    //_logger.Log(LogFlags.EntityId, $"NextEntityId is '{_nextEntityId}'");

    ArchetypeDataRecord record = _recordPool.Get();
    record.Init(id, archetype, _componentFactory, _archetypeGraph, _componentDefinitions);
    
    return CreateEntityInternal(record);
  }

  private ArchetypeDataTable InsertNewTable(Archetype archetype)
  {
    ArchetypeDataTable table = new ArchetypeDataTable(archetype);
    _tables.Add(archetype, table);
    _trackedArchetypes.Add(archetype);

    return table;
  }

  private ArchetypeDataRecord CreateEntityInternal(IEntityData data)
  {
    ArchetypeDataRecord record = _recordPool.Get();
    record.Init(data.GetEntityId(), data.GetArchetype(), _componentFactory, _archetypeGraph, _componentDefinitions);
    
    foreach (var cIdx in _archetypeGraph.GetComponentIndicesForArchetype(data.GetArchetype()))
    {
      IComponent toCopy = data.GetComponent(cIdx);
      IComponent copyTo = record.GetComponent(cIdx);
      _componentFactory.Copy(toCopy, copyTo);
    }
    
    return CreateEntityInternal(record);
  }

  private ArchetypeDataRecord CreateEntityInternal(ArchetypeDataRecord record)
  {
    ArchetypeDataTable table;
    if (!_tables.TryGetValue(record.GetArchetype(), out table))
    {
      table = InsertNewTable(record.GetArchetype());
    }

    _allEntityIds.Add(record.GetEntityId());
    _allEntities.Add(record);
    _allRecords.Add(record);
    _createdThisFrame.Add(record.GetEntityId());
    _entityArchetypeLookup.Add(record.GetEntityId(), record.GetArchetype());
    table.AddRecord(record);
    return record;
  }

  private T AddToEntityInternal<T>(EntityId entityId) where T : IComponent, new()
  {
    IComponent added = AddToEntityInternal(entityId, typeof(T));
    return (T) added;
  }
  
  private IComponent AddToEntityInternal(EntityId entityId, Type compType)
  {
    if (!_entityArchetypeLookup.TryGetValue(entityId, out Archetype curArchetype))
    {
      throw new ArgumentException();
    }

    ArchetypeDataRecord record = _tables[curArchetype].RemoveRecord(entityId);
    ComponentTypeIndex componentTypeIndex = _componentDefinitions.GetIndex(compType);
    record.AddComponentType(_componentDefinitions.GetIndex(compType), _componentFactory.Get(componentTypeIndex));

    Archetype newArchetype = record.GetArchetype();
    _entityArchetypeLookup[entityId] = newArchetype;
    
    ArchetypeDataTable newTable;
    if (!_tables.TryGetValue(newArchetype, out newTable))
    {
      newTable = InsertNewTable(newArchetype);
    }
    
    newTable.AddRecord(record);
    return record.Get(componentTypeIndex);
  }
  
  private void RemoveMatchingFromEntityInternal(EntityId entityId, Archetype filterArchetype)
  {
    if (!_entityArchetypeLookup.TryGetValue(entityId, out Archetype curArchetype))
    {
      throw new ArgumentException();
    }

    if (!curArchetype.Overlaps(filterArchetype))
    {
      return;
    }

    ArchetypeDataRecord record = _tables[curArchetype].RemoveRecord(entityId);
    record.RemoveOverlapping(filterArchetype);

    Archetype newArchetype = record.GetArchetype();
    _entityArchetypeLookup[entityId] = newArchetype;
    
    ArchetypeDataTable newTable;
    if (!_tables.TryGetValue(newArchetype, out newTable))
    {
      newTable = InsertNewTable(newArchetype);
    }
    
    newTable.AddRecord(record);
  }
  
  private void DestroyAllOverlappingInternal(Archetype filterArchetype)
  {
    for (int i = _trackedArchetypes.Count - 1; i >= 0; i--)
    {
      Archetype curArchetype = _trackedArchetypes[i];
      if (curArchetype.Overlaps(filterArchetype))
      {
        List<EntityId> entities = _tables[curArchetype].GetEntities();
        for(int j = entities.Count - 1; j >= 0; j--)
        {
          DestroyEntity(entities[j]);
        }
      }
    }
  }
  
  private void RemoveAllMatchingInternal(Archetype filterArchetype)
  {
    for (int i = _trackedArchetypes.Count - 1; i >= 0; i--)
    {
      Archetype curArchetype = _trackedArchetypes[i];
      if (curArchetype.Overlaps(filterArchetype))
      {
        List<ArchetypeDataRecord> tempList = new List<ArchetypeDataRecord>();
        _tables[curArchetype].ExtractAllRecords(tempList);

        foreach (var recordToTransform in tempList)
        {
          recordToTransform.RemoveOverlapping(filterArchetype);
          Archetype newArchetype = recordToTransform.GetArchetype();
          _entityArchetypeLookup[recordToTransform.GetEntityId()] = newArchetype;
          
          ArchetypeDataTable newTable;
          if (!_tables.TryGetValue(newArchetype, out newTable))
          {
            newTable = InsertNewTable(newArchetype);
          }
    
          newTable.AddRecord(recordToTransform);
        }
      }
    }
  }

  public IEntityData GetEntityData(EntityId id)
  {
    Archetype a = _entityArchetypeLookup[id];
    return _tables[a].GetRecord(id);
  }

  public bool Exists(EntityId id)
  {
    return _entityArchetypeLookup.ContainsKey(id);
  }

  private void AddRecordsContainingArchetype(Archetype a, IQueryResult result)
  {
    _tempArchetypeList.Clear();
    foreach (var archetype in _trackedArchetypes)
    {
      if (Archetype.IsSubsetOf(a, archetype))
      {
        _tempArchetypeList.Add(archetype);
      }
    }
    
    foreach (var sArchetype in _tempArchetypeList)
    {
      _tables[sArchetype].PopulateResults(result); 
    }
  }
  
  private void FilterOutRecordsWithComponentsNotMatchingFieldKey(ComponentTypeIndex matchesComponentFieldKeyCompIndex,
                                                      object             matchesComponentFieldKeyValue, 
                                                      IQueryResult result)
  {
    _tempIdList.Clear();
    foreach (var entityData in result.GetRecords())
    {
      EntityId eId = entityData.GetEntityId();
      if (!_entityArchetypeLookup.TryGetValue(eId, out Archetype a))
      {
       continue;
      }
      
      if (a.Contains(matchesComponentFieldKeyCompIndex))
      {
        IEntityData e = _tables[a].GetRecord(eId);
        IComponent c = e.GetComponent(matchesComponentFieldKeyCompIndex);
        if (!_componentDefinitions.MatchesComponentFieldKey(c, matchesComponentFieldKeyValue))
        {
          _tempIdList.Add(e.GetEntityId());
        }
      }
    }

    foreach (var toRemove in _tempIdList)
    {
      result.RemoveRecord(toRemove);
    }
  }

  public void PrepareNextFrame(IFrameInputData inputFrameData)
  {
    _createdThisFrame.Clear();
    ClearInputEntities();
    foreach (var componentGroup in inputFrameData.GetComponentGroups())
    {
      CreateEntityInternal(componentGroup).GetEntityId();
    }
    
    DestroyEntitiesWithAnyOverlap(_archetypeGraph.GetSingleFrameArchetype());
  }

  internal void ClearInputEntities()
  {
    List<Archetype> inputArchs = _archetypeGraph.GetInputArchetypes();

    foreach (var inputArch in inputArchs)
    {
      if (!_tables.TryGetValue(inputArch, out ArchetypeDataTable table))
      {
        continue;
      }

      var inputEntities = table.GetEntities();
      for(int i = inputEntities.Count - 1; i >= 0; i--)
      {
        DestroyEntity(inputEntities[i]);
      } 
    }
  }

  public void CopyTo(IComponent source, IComponent target)
  {
    _componentFactory.Copy(source, target);
  }

  public List<EntityId> GetEntityIds()
  {
    return _allEntityIds;
  }

  public Archetype GetEntityArchetype(EntityId id)
  {
    return _entityArchetypeLookup[id];
  }

  public void ClearAndCopy(IEntityFrameSnapshot data)
  {
    for(int i = _allEntityIds.Count - 1; i >= 0; i--)
    {
      DestroyEntity(_allEntityIds[i]);
    }
    
    _nextEntityId = data.GetNextEntityId().Id;
    
    //_logger.Log(LogFlags.EntityId, $"Set NextEntityId to '{_nextEntityId}'");

    foreach (var entityData in data.GetEntitiesData())
    {
      CreateEntityInternal(entityData);
    }

    //if singleton archetype is empty, current serialization leaves it out...
    if (!_entityArchetypeLookup.ContainsKey(new EntityId(1)))
    {
      EntityId id = new EntityId(1);
      ArchetypeDataRecord record = _recordPool.Get();
      record.Init(id, _singletonArchetype, _componentFactory, _archetypeGraph, _componentDefinitions);
    
      CreateEntityInternal(record);
    }
    
    //TODO: get new ids directly, this is inefficient
    _createdThisFrame.Clear();
    foreach (var e in _allEntityIds)
    {
      if (data.IsNewEntity(e))
      {
        _createdThisFrame.Add(e);
      }
    }
  }

  public void RunQuery(Query query)
  {
    if ((query.FilterFlags & QueryFilter.ContainsArchetype) == QueryFilter.ContainsArchetype)
    {
      AddRecordsContainingArchetype(query.ContainsThisArchetype, query);
    }
    else
    {
      foreach (var eId in _allEntityIds)
      {
        query.AddRecord(GetEntityData(eId));
      }
    }
    
    if ((query.FilterFlags & QueryFilter.MatchesComponentFieldKey) == QueryFilter.MatchesComponentFieldKey)
    {
      FilterOutRecordsWithComponentsNotMatchingFieldKey(query._matchesComponentFieldKeyCompIndex, 
                                              query._matchesComponentFieldKeyValue, 
                                              query);
    }
  }

  public bool EntityExists(EntityId entityId)
  {
    return _allEntityIds.Contains(entityId);
  }

  public List<IEntityData> GetEntitiesData()
  {
    return _allEntities;
  }
  
  public string GetStateString()
  {
    StringBuilder sb = new StringBuilder();
    foreach (var e in _allEntities)
    {
      sb.AppendLine("----------------");
      sb.AppendLine($"Entity '{e.GetEntityId().Id}':");
      foreach (var cTypeIndex in _archetypeGraph.GetComponentIndicesForArchetype(e.GetArchetype()))
      {
        sb.AppendLine(_componentFactory.ToString(e.GetComponent(cTypeIndex)));
      }
      
      sb.AppendLine("----------------");
    }

    return sb.ToString();
  }

  public EntityId GetNextEntityId()
  {
    return new EntityId(_nextEntityId);
  }
}


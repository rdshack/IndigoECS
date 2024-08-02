namespace ecs;


public class DeserializedFrameDataStore : IDeserializedFrameDataStore
{
  public int                   FrameNum             { get; set; }
  public EntityId              NextEntityId         { get; set; }
  public IComponentDefinitions ComponentDefinitions { get; private set; }
  public IComponentFactory     ComponentPool        { get; private set; }
  
  private List<EntityData>                 _entityData;
  private Dictionary<EntityId, EntityData> _entityDataLookup;
  private Dictionary<EntityId, int>     _newEntityHash = new Dictionary<EntityId, int>();
  private ArchetypeGraph                   _archetypeGraph;
  private HashSet<EntityId>                _newEntities = new HashSet<EntityId>();

  public void Setup(IComponentFactory pool, IComponentDefinitions componentDefinitions, ArchetypeGraph graph)
  {
    ComponentPool = pool;
    ComponentDefinitions = componentDefinitions;
    _archetypeGraph = graph;
    _entityData = new List<EntityData>();
    _entityDataLookup = new Dictionary<EntityId, EntityData>();
  }
  

  public List<EntityData> GetEntities()
  {
    return _entityData;
  }
  
  public void AddEntity(EntityId entityId, bool isNew)
  {
    var entityData = ComponentPool.GetEntityData();
    entityData.Setup(entityId, ComponentPool, _archetypeGraph, ComponentDefinitions);
    entityData.EntityId = entityId;
    _entityDataLookup[entityId] = entityData;
    _entityData.Add(entityData);

    if (isNew)
    {
      _newEntities.Add(entityId); 
    }
  }

  public void AddComponent(EntityId entityId, IComponent c)
  {
    _entityDataLookup[entityId].AddComponent(c);
  }

  public void SetNewEntityHash(EntityId entityId, int hash)
  {
    _newEntities.Add(entityId);
    _newEntityHash.Add(entityId, hash);
  }

  public void Reset()
  {
    FrameNum = -1;
    NextEntityId = default;

    for(int i = _entityData.Count - 1; i >= 0; i--)
    {
      _entityData[i].Dispose();
      ComponentPool.ReturnEntityData(_entityData[i]);
      _entityData.RemoveAt(i);
    }
    
    _entityData.Clear();
    _newEntities.Clear();
    _newEntityHash.Clear();
    _entityDataLookup.Clear();
  }

  public EntityId GetNextEntityId()
  {
    return NextEntityId;
  }

  public List<IEntityData> GetEntitiesData()
  {
    return new List<IEntityData>(_entityData);
  }

  public IComponent GetEntityComponent(EntityId id, ComponentTypeIndex idx)
  {
    return _entityDataLookup[id].GetComponent(idx);
  }

  public bool IsNewEntity(EntityId getEntityId)
  {
    return _newEntities.Contains(getEntityId);
  }

  public void Dispose()
  {
    Reset();
  }
}
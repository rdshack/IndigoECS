namespace ecs;


public class EntityFrameSnapshot : IEntityFrameSnapshot
{
  public int                   FrameNum             { get; set; }
  public EntityId              NextEntityId         { get; set; }
  public IComponentDefinitions ComponentDefinitions { get; private set; }
  public IComponentFactory     ComponentPool        { get; private set; }

  private List<IEntityData>                _entityData       = new List<IEntityData>();
  private Dictionary<EntityId, EntityData> _entityDataLookup = new Dictionary<EntityId, EntityData>();
  private ArchetypeGraph                   _archetypeGraph;
  private HashSet<EntityId>                _newEntities = new HashSet<EntityId>();
  
  public void Init(IComponentFactory     pool,
                   IComponentDefinitions componentDefinitions,
                   ArchetypeGraph        graph)
  {
    ComponentPool = pool;
    ComponentDefinitions = componentDefinitions;
    _archetypeGraph = graph;
  }

  public List<IEntityData> GetEntities()
  {
    return _entityData;
  }
  
  public void AddEntity(EntityId entityId, bool isNew, Archetype archetype, List<IComponent> components)
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
    
    entityData.SetComponents(archetype, components);
  }
  
  public EntityId GetNextEntityId()
  {
    return NextEntityId;
  }

  public List<IEntityData> GetEntitiesData()
  {
    return _entityData;
  }

  public void ClearEntityList()
  {
    _entityData.Clear();
  }

  public IComponent GetEntityComponent(EntityId id, ComponentTypeIndex idx)
  {
    return _entityDataLookup[id].GetComponent(idx);
  }

  public bool IsNewEntity(EntityId getEntityId)
  {
    return _newEntities.Contains(getEntityId);
  }

  public void Reset()
  {
    foreach (var entityData in _entityData)
    {
      entityData.Reset();
      ComponentPool.ReturnEntityData(entityData);
    }
    
    _entityData.Clear();
    _entityDataLookup.Clear();
    _newEntities.Clear();

    FrameNum = 0;
    NextEntityId = new EntityId(0);
  }

  public static EntityFrameSnapshot Build()
  {
    return new EntityFrameSnapshot();
  }

  public static void Reset(EntityFrameSnapshot obj)
  {
    obj.Reset();
  }
}
namespace ecs;

public interface IFrameData
{
  int                  GetFrameNum();
  IEntityFrameSnapshot GetEntityRepo();
  bool                 IsNewEntity(EntityId getEntityId);
  void Reset();
}

public interface IDeserializedFrameDataStore : IEntityFrameSnapshot
{
  int                  FrameNum             { get; set; }
  IComponentDefinitions ComponentDefinitions { get; }
  IComponentFactory    ComponentPool        { get; }
  EntityId             NextEntityId         { get; set; }
  void                 AddEntity(EntityId        entityId, bool       isNew);
  void                 AddComponent(EntityId     entityId, IComponent c);
  void                 SetNewEntityHash(EntityId entityId, int        hash);
}

public interface IEntityFrameSnapshot
{
  EntityId          GetNextEntityId();
  List<IEntityData> GetEntitiesData();
  IComponent        GetEntityComponent(EntityId id, ComponentTypeIndex idx);
  bool              IsNewEntity(EntityId        getEntityId);
  void              Reset();
}

public interface IDeserializedFrameSyncStore : IFrameSyncData
{
  int                  FrameNum             { get; set; }
  int                  FullStateHash        { get; set; }
  IComponentDefinitions ComponentDefinitions { get; }
  IComponentFactory    ComponentPool        { get; }
  void                 Reset();
  void                 AddComponent(EntityId entityId, IComponent c);
}

public interface IFrameSyncData
{
  int  GetFrameNum();
  int  GetFullStateHash(); 
  List<IComponentGroup> GetClientInputData();
  void Dispose();
}

public class FrameData : IFrameData
{
  private int                  _frameNum;
  private EntityId             _nextEntityId;
  private IEntityFrameSnapshot _repo;
  private          List<IComponent>     _tempComponentList = new List<IComponent>();

  private FrameData()
  {
  }

  public void Init(int num, EntityId nextEntityId, IEntityFrameSnapshot repo)
  {
    _frameNum = num;
    _nextEntityId = nextEntityId;
    _repo = repo;
  }
  
  public int GetFrameNum()
  {
    return _frameNum;
  }

  public IEntityFrameSnapshot GetEntityRepo()
  {
    return _repo;
  }

  public bool IsNewEntity(EntityId getEntityId)
  {
    return _repo.IsNewEntity(getEntityId);
  }

  public void Clear()
  {
    _repo = null;
    _tempComponentList.Clear();
  }

  public void Reset()
  {
    _repo?.Reset();
    _tempComponentList.Clear();
  }

  public void Clone(IComponentFactory     pool, 
                          ArchetypeGraph        graph, 
                          IComponentDefinitions defs,
                          FrameData cloneTarget)
  {
    EntityFrameSnapshot dataStore = new EntityFrameSnapshot();
    dataStore.Init(pool, defs, graph);

    dataStore.FrameNum = _frameNum;
    dataStore.NextEntityId = _nextEntityId;
    foreach (var entityDataToClone in _repo.GetEntitiesData())
    {
      _tempComponentList.Clear();
      List<IComponent> toCloneList = entityDataToClone.GetAllComponents();
      foreach (var toCopy in toCloneList)
      {
        ComponentTypeIndex idx = defs.GetIndex(toCopy);
        IComponent copyTarget = pool.Get(idx);
        pool.Copy(toCopy, copyTarget);
        _tempComponentList.Add(copyTarget);
      }
      
      dataStore.AddEntity(entityDataToClone.GetEntityId(), 
                          IsNewEntity(entityDataToClone.GetEntityId()),
                          entityDataToClone.GetArchetype(), 
                          _tempComponentList);
    }
    
    
    cloneTarget.Init(_frameNum, _nextEntityId, dataStore);
  }

  public static FrameData Create()
  {
    return new FrameData();
  }

  public static void Reset(FrameData obj)
  {
    obj.Reset();
  }
}

public class FrameSyncData : IFrameSyncData
{
  private int                   _frameNum;
  private int                   _fullStateHash;
  private List<IComponentGroup> _entitiesData = new List<IComponentGroup>();
  private IComponentFactory     _pool;

  private FrameSyncData()
  {
  }

  public void Init(int num, int fullStateHash, List<IComponentGroup> entitiesData, IComponentFactory pool)
  {
    _fullStateHash = fullStateHash;
    _frameNum = num;
    _entitiesData = entitiesData;
    _pool = pool;
  }
  
  public void Init(int num, int fullStateHash, List<IEntityData> entitiesData, IComponentFactory pool)
  {
    _fullStateHash = fullStateHash;
    _frameNum = num;
    _entitiesData.Clear();
    _entitiesData.AddRange(entitiesData);
    _pool = pool;
  }

  public int GetFrameNum()
  {
    return _frameNum;
  }

  public int GetFullStateHash()
  {
    return _fullStateHash;
  }

  public List<IComponentGroup> GetClientInputData()
  {
    return _entitiesData;
  }

  public void Dispose()
  {
    foreach (var componentGroup in _entitiesData)
    {
      componentGroup.Reset();
      _pool.ReturnEntityData(componentGroup);
    }
  }

  public static FrameSyncData Create()
  {
    return new FrameSyncData();
  }

  public static void Reset(FrameSyncData obj)
  {
    obj.Dispose();
  }
}
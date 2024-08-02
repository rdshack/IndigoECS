namespace ecs;
/*
public class ComponentPool
{
  private const int DEFAULT_POOL_SIZE = 5;

  private string                                              _id;
  private ComponentDefinitions                               _componentDefinitions;
  private ComponentFactory                                    _componentCopier;
  private Dictionary<ComponentTypeIndex, ObjPool<Component>> _pools;
  private ObjPool<EntityData>                                 _entityDataPool;
  private ObjPool<ComponentGroup>                             _compGroupPool;

  public ComponentPool(ComponentFactory copier, ComponentDefinitions componentDefinitions, string id)
  {
    _componentCopier = copier;
    _componentDefinitions = componentDefinitions;
    _pools = new Dictionary<ComponentTypeIndex, ObjPool<Component>>();

    _id = id;
    _entityDataPool = new ObjPool<EntityData>(EntityData.Create, EntityData.Reset);
    _compGroupPool = new ObjPool<ComponentGroup>(ComponentGroup.Create, ComponentGroup.Reset);

    foreach (var componentTypeIndex in _componentDefinitions.GetTypeIndices())
    {
      var pool = new ObjPool<Component>(_componentCopier.c)
      _pools[componentTypeIndex] = pool;
      _poolSizes[componentTypeIndex] = DEFAULT_POOL_SIZE;
      Type t = _componentDefinitions.GetComponentType(componentTypeIndex);

      for (int i = 0; i < DEFAULT_POOL_SIZE; i++)
      {
        pool.Push(Build(t)); 
      }
    }
  }
  
  public EntityData GetEntityData()
  {
    return _entityDataPool.Get();
  }

  public void ReturnEntityData(ComponentGroup e)
  {
    if (e is EntityData entityData)
    {
      _entityDataPool.Return(entityData);
    }
    else if (e is ComponentGroup componentGroup)
    {
      _compGroupPool.Return(componentGroup);
    }
    else
    {
      throw new Exception();
    }
  }
  
  public ComponentGroup GetComponentGroup(ComponentPool pool, ArchetypeGraph graph, ComponentDefinitions definitions)
  {
    ComponentGroup group = _compGroupPool.Get();
    group.Setup(pool, graph, definitions);
    return group;
  }

  public Component Get(ComponentTypeIndex idx)
  {
    Type t = _componentDefinitions.GetComponentType(idx);
    
    var pool = _pools[idx];

    Component component;
    if (pool.Count != 0)
    {
      component = pool.Pop();
      _inUse.Add(component);
      return component;
    }

    int curSize = _poolSizes[idx];
    for (int i = 0; i < curSize; i++)
    {
      pool.Push(Build(t)); 
    }

    _poolSizes[idx] *= 2;
    
    component = pool.Pop();
    _inUse.Add(component);
    return component;
  }

  public void Return(Component component)
  {
    if (_inUse.Remove(component))
    {
      _componentCopier.Reset(component);
      ComponentTypeIndex idx = _componentDefinitions.GetIndex(component);
      _pools[idx].Push(component);
    }
    else
    {
      throw new ArgumentException();      
    }
  }

  private Component Build(Type t)
  {
    return (Component)Activator.CreateInstance(t);
  }

  public void ReturnAll()
  {
    for (int i = _inUse.Count - 1; i >= 0; i--)
    {
      Return(_inUse[i]);
    }
  }
}*/
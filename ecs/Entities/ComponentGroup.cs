namespace ecs;


public class ComponentGroup : IComponentGroup
{
  private Dictionary<ComponentTypeIndex, IComponent> _components = new Dictionary<ComponentTypeIndex, IComponent>();
  private List<IComponent>                           _componentList = new List<IComponent>();
  private IComponentDefinitions                      _componentDefinitions;
  private Archetype                                  _curArchetype;
  private ArchetypeGraph                             _archetypeGraph;
  private IComponentFactory                          _componentPool;


  public ComponentGroup()
  {

  }

  public void Setup(IComponentFactory     componentPool, 
                    ArchetypeGraph        archetypeGraph, 
                    IComponentDefinitions componentDefinitions)
  {
    _curArchetype = archetypeGraph.GetEmpty();
    _componentPool = componentPool;
    _archetypeGraph = archetypeGraph;
    _componentDefinitions = componentDefinitions;
  }

  public List<IComponent> GetAllComponents()
  {
    return _componentList;
  }

  public Archetype GetArchetype()
  {
    return _curArchetype;
  }

  public IComponent GetComponent(ComponentTypeIndex idx)
  {
    return _components[idx];
  }

  public T Get<T>() where T : IComponent, new()
  {
    ComponentTypeIndex idx = _componentDefinitions.GetIndex<T>();
    return (T)_components[idx];
  }

  public void Dispose()
  {
    Reset();
  }

  public void Reset()
  {
    foreach (var idx in _components.Keys)
    {
      _componentPool.Return(_components[idx]);
    }
    
    _components.Clear();
    _componentList.Clear();
  }

  public void AddComponentType(ComponentTypeIndex idx, IComponent component)
  {
    if (_components.TryAdd(idx, component))
    {
      _componentList.Add(component);
      _curArchetype = _archetypeGraph.GetArchetypeIfAdded(_curArchetype, idx);
    }
    else
    {
      throw new Exception();
    }
  }

  public static void Reset(ComponentGroup obj)
  {
    obj.Reset();
  }
  
  public static ComponentGroup Create()
  {
    return new ComponentGroup();
  }
}
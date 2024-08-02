
using System.Diagnostics;
using System.Text;

namespace ecs;

public class EntityData : IEntityData
{
  public static int _nextInstanceId = 1;
  public        int InstanceId;
  
  public EntityId EntityId { get; set; }

  private Archetype             _curArchetype;
  private IComponentDefinitions _componentDefinitions;
  private ArchetypeGraph        _archetypeGraph;
  private IComponentFactory     _componentPool;
  
  private Dictionary<ComponentTypeIndex, IComponent> _componentData = new Dictionary<ComponentTypeIndex, IComponent>();
  private List<IComponent>                           _components    = new List<IComponent>();

  public EntityData()
  {
    InstanceId = _nextInstanceId++;
  }
  
  public void Setup(EntityId              id, 
                    IComponentFactory     componentPool, 
                    ArchetypeGraph        archetypeGraph, 
                    IComponentDefinitions componentDefinitions)
  {
    EntityId = id;
    _curArchetype = archetypeGraph.GetEmpty();
    _componentPool = componentPool;
    _archetypeGraph = archetypeGraph;
    _componentDefinitions = componentDefinitions;
  }
  
  public void AddComponent(IComponent component)
  {
    var idx = _componentDefinitions.GetIndex(component);
    _componentData.Add(idx, component);
    _components.Add(component);
    _curArchetype = _archetypeGraph.GetArchetypeIfAdded(_curArchetype, idx);
  }
  
  public void SetComponents(Archetype a, List<IComponent> components)
  {
    Reset();
    _curArchetype = a;
    foreach (var c in components)
    {
      _componentData.Add(_componentDefinitions.GetIndex(c), c);
      _components.Add(c);
    }
  }

  public List<IComponent> GetAllComponents()
  {
    return _components;
  }

  public Archetype GetArchetype()
  {
    return _curArchetype;
  }

  public IComponent GetComponent(ComponentTypeIndex idx)
  {
    return _componentData[idx];
  }

  public T Get<T>() where T : IComponent, new()
  {
    ComponentTypeIndex idx = _componentDefinitions.GetIndex<T>();
    return (T)_componentData[idx];
  }

  public void Dispose()
  {
    Reset();
  }

  public EntityId GetEntityId()
  {
    return EntityId;
  }

  public void Reset()
  {
    foreach (IComponent c in _components)
    {
      _componentPool.Return(c);
    }
    
    _componentData.Clear();
    _components.Clear();
    _curArchetype = _archetypeGraph.GetEmpty();
  }
  
  public static void Reset(EntityData obj)
  {
    obj.Reset();
  }
  
  public static EntityData Create()
  {
    return new EntityData();
  }
}


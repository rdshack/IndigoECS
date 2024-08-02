using System.Collections;

namespace ecs;

internal class ArchetypeDataRecord : IEntityData
{
  private EntityId                                   _entityId;
  private Archetype                                  _curArchetype;
  private IComponentDefinitions                      _componentDefinitions;
  private ArchetypeGraph                             _archetypeGraph;
  private Dictionary<ComponentTypeIndex, IComponent> _componentData = new Dictionary<ComponentTypeIndex, IComponent>();
  private List<IComponent>                           _components    = new List<IComponent>();
  private IComponentFactory                          _componentPool;

  private ArchetypeDataRecord()
  {
  }

  public void Init(EntityId id, Archetype archetype,
                   IComponentFactory componentPool, 
                   ArchetypeGraph archetypeGraph, 
                   IComponentDefinitions componentDefinitions)
  {
    _entityId = id;
    _curArchetype = archetype;
    _componentPool = componentPool;
    _archetypeGraph = archetypeGraph;
    _componentDefinitions = componentDefinitions;

    var componentIndices = _archetypeGraph.GetComponentIndicesForArchetype(archetype);
    foreach (var componentTypeIndex in componentIndices)
    {
      var component = _componentPool.Get(componentTypeIndex);
      _componentData.Add(componentTypeIndex, component);
      _components.Add(component);
    }
  }
  
  public EntityId GetEntityId()
  {
    return _entityId;
  }

  internal void AddComponentType(ComponentTypeIndex idx, IComponent component)
  {
    _componentData.Add(idx, component);
    _components.Add(component);
    _curArchetype = _archetypeGraph.GetArchetypeIfAdded(_curArchetype, idx);
  }
  
  internal void RemoveOverlapping(Archetype archetype)
  {
    foreach (var idx in _archetypeGraph.GetComponentIndicesForArchetype(archetype))
    {
      var component = _componentData[idx];
      _componentPool.Return(component);
      _componentData.Remove(idx);
      _components.Remove(component);
    }
    
    _curArchetype = _archetypeGraph.GetArchetypeIfRemoved(_curArchetype, archetype);
  }
  
  internal void ReturnAllComponents()
  {
    foreach (var idx in _componentData.Keys)
    {
      _componentPool.Return(_componentData[idx]);
    }
    
    _componentData.Clear();
    _components.Clear();
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

  internal bool TryGet<T>(out T component) where T : IComponent, new()
  {
    ComponentTypeIndex idx = _componentDefinitions.GetIndex<T>();
    if (_componentData.TryGetValue(idx, out IComponent c))
    {
      component = (T) c;
      return true;
    }

    component = default;
    return false;
  }
  
  public T Get<T>() where T : IComponent, new()
  {
    ComponentTypeIndex idx = _componentDefinitions.GetIndex<T>();
    return (T)_componentData[idx];
  }

  public void Reset()
  {
    _entityId = default;
    _curArchetype = null;
    _componentDefinitions = null;
    _archetypeGraph = null;
    _componentPool = null;
    _components.Clear();
    _componentData.Clear();
  }

  internal IComponent Get(ComponentTypeIndex idx)
  {
    return _componentData[idx];
  }

  internal static ArchetypeDataRecord Create()
  {
    return new ArchetypeDataRecord();
  }

  internal static void Reset(ArchetypeDataRecord obj)
  {
    obj.Reset();
  }
}
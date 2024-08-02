namespace ecs;

/// <summary>
/// List of 'component groups'. 
/// </summary>
public interface IComponentGroupsData
{
  IEnumerable<IComponentGroup> GetComponentGroups();
}


/// <summary>
/// Each 'component group' represents the set of
/// data that could be associated with an entity. External input is passed in as
/// a component group, and then assigned to an entity upon input being received.
/// </summary>
public interface IComponentGroup
{
  List<IComponent> GetAllComponents();
  Archetype               GetArchetype();
  IComponent              GetComponent(ComponentTypeIndex idx);
  T                       Get<T>() where T : IComponent, new();
  void Reset();
}


/// <summary>
/// Represents all the input for a given frame, before it has been assigned to entities
/// </summary>
public interface IFrameInputData : IComponentGroupsData
{
  int  GetFrameNum();
}

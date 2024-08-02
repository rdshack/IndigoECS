namespace ecs;

public interface IComponentFactory
{
  EntityData     GetEntityData();
  void           ReturnEntityData(IComponentGroup e);
  ComponentGroup GetComponentGroup(ArchetypeGraph graph, IComponentDefinitions definitions);
  
  IComponent     Get(ComponentTypeIndex idx);
  void           Return(IComponent component);
  void           ReturnAll();

  //void SetDebugFlag(ComponentFactoryDebugFlag f);
  
  void   Copy(IComponent     source, IComponent target);
  void   Reset(IComponent    c);
  string ToString(IComponent c);
}

[Flags]
public enum ComponentFactoryDebugFlag
{
  EntityData = 1
}
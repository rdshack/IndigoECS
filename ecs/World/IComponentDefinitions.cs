namespace ecs;

public interface IComponentDefinitions
{
  IEnumerable<ComponentTypeIndex> GetTypeIndices();
  Type                            GetComponentType(ComponentTypeIndex idx);
  ComponentTypeIndex              GetIndex(IComponent                 c);
  ComponentTypeIndex              GetIndex(Type                       cType);
  ComponentTypeIndex              GetIndex<T>() where T : IComponent, new();
  bool                            IsSingletonComponent(ComponentTypeIndex idx);
  List<ComponentTypeIndex>        GetAllSingletonComponents();
  List<ComponentTypeIndex>        GetAllSingleFrameComponents();
  bool                            MatchesComponentFieldKey(IComponent  c, object     val);
  int                             CompareComponentFieldKeys(IComponent a, IComponent b);
}
namespace ecs;

public interface IAliasLookup
{
  IEnumerable<ComponentTypeIndex> GetAssociatedComponents(AliasId id);
  IEnumerable<AliasId>            GetInputAlias();
  ComponentTypeIndex              GetInputAliasKeyComponent(AliasId id);
  AliasId                         GetAliasForArchetype(Archetype    a);
}
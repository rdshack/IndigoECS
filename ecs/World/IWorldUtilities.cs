namespace ecs;

public interface IWorldUtilities
{
  IComponentDefinitions GetComponentIndex();
  IAliasLookup          GetAliasDefinition();
  IFrameSerializer      GetSerializer();
  IComponentFactory     BuildComponentFactory();
  IWorldLogger          GetLogger();
}
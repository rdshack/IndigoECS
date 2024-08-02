namespace ecs;

public interface IComponentFieldKeyMatcher
{
  bool MatchesComponentFieldKey(ComponentTypeIndex componentTypeIndex, object val);
}
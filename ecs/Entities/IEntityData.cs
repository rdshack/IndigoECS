namespace ecs;

/// <summary>
/// ComponentGroupsData, but with entity id association
/// </summary>
/*public interface IEntitiesDatabase
{
  IReadOnlyList<IEntityData> GetDataList();
  Component                 GetEntityComponent(EntityId             getEntityId, ComponentTypeIndex componentTypeIndex);
  Archetype                  GetEntityArchetype(EntityId             id);
  IEnumerable<EntityId>      GetEntityIds();

  T    GetSingletonComponent<T>() where T : Component, new();
  T    GetEntityComponent<T>(EntityId    id) where T : Component, new();
  bool TryGetEntityComponent<T>(EntityId id, out T component) where T : Component, new();
  
  //Query util
  void                       AddRecordsContainingArchetype(Archetype a,           List<IEntityData>  results);
  void                       AddRecordsWithComponentMatchingFieldKey(ComponentTypeIndex matchesComponentFieldKeyCompIndex, 
                                                                     object matchesComponentFieldKeyValue, 
                                                                     List<IEntityData> records);
}*/

/// <summary>
/// Set of component data
/// </summary>
public interface IEntityData : IComponentGroup
{
  EntityId GetEntityId();
}

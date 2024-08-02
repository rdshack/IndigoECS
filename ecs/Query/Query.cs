namespace ecs;

[Flags]
public enum QueryFilter
{
  None,
  ContainsArchetype,
  MatchesComponentFieldKey
}

public interface IQueryResult
{
  void              AddRecord(IEntityData record);
  List<IEntityData> GetRecords();
  void              RemoveRecord(EntityId eid);
}

public interface IQueryRunner
{
  void RunQuery(Query q);
}

public class Query : IQueryResult
{
  private ArchetypeGraph        _archetypeGraph;
  private IComponentDefinitions _compDefinitions;
  public  QueryFilter           FilterFlags;
  
  private List<IEntityData>                 _records    = new List<IEntityData>();
  private Dictionary<EntityId, IEntityData> _recordById = new Dictionary<EntityId, IEntityData>();

  public Archetype          ContainsThisArchetype;
  public ComponentTypeIndex _matchesComponentFieldKeyCompIndex;
  public object             _matchesComponentFieldKeyValue;

  public Query(ArchetypeGraph w, IComponentDefinitions definitions)
  {
    _archetypeGraph = w;
    _compDefinitions = definitions;
  }

  public void Clear()
  {
    FilterFlags = 0;
  }

  public Query SetContainsArchetypeFilter(Archetype archetype)
  {
    FilterFlags |= QueryFilter.ContainsArchetype;
    ContainsThisArchetype = archetype;
    return this;
  }
  
  public Query SetMatchesComponentFieldKeyFilter<T>(object val) where T : IComponent, new()
  {
    return SetMatchesComponentFieldKeyFilter(_compDefinitions.GetIndex<T>(), val);
  }
  
  public Query SetMatchesComponentFieldKeyFilter(ComponentTypeIndex index, object val)
  {
    FilterFlags |= QueryFilter.MatchesComponentFieldKey;
    _matchesComponentFieldKeyCompIndex = index;
    _matchesComponentFieldKeyValue = val;
    return this;
  }
  
  public Query SetContainsAliasFilter(AliasId alias)
  {
    return SetContainsArchetypeFilter(_archetypeGraph.GetAliasArchetype(alias));
  }

  public void AddRecord(IEntityData record)
  {
    if (_recordById.ContainsKey(record.GetEntityId()))
    {
      return;
    }

    _recordById[record.GetEntityId()] = record;
    _records.Add(record);
  }

  public List<IEntityData> GetRecords()
  {
    return _records;
  }

  public void RemoveRecord(EntityId eid)
  {
    if (_recordById.TryGetValue(eid, out IEntityData data))
    {
      _recordById.Remove(eid);
      _records.Remove(data);
    }
  }

  public List<IEntityData> Resolve(IQueryRunner dataSource)
  {
    ClearRecords();
    dataSource.RunQuery(this);
    return _records;
  }

  public void ClearRecords()
  {
    _records.Clear();
    _recordById.Clear();
  }

}
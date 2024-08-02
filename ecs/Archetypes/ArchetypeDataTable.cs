
namespace ecs;

internal class ArchetypeDataTable
{
  private Dictionary<EntityId, ArchetypeDataRecord> _entityToRecord;
  private List<EntityId>                            _entityList;
  private List<ArchetypeDataRecord>                 _recordList;
  private Archetype                                 _archetype;

  internal ArchetypeDataTable(Archetype a)
  {
    _entityToRecord = new Dictionary<EntityId, ArchetypeDataRecord>();
    _recordList = new List<ArchetypeDataRecord>();
    _entityList = new List<EntityId>();
    _archetype = a;
  }

  internal Archetype GetArchetype()
  {
    return _archetype;
  }

  internal List<ArchetypeDataRecord> GetRecords()
  {
    return _recordList;
  }
  
  internal IEnumerable<EntityId> GetEntityIds()
  {
    return _entityList;
  }

  internal void AddRecord(ArchetypeDataRecord record)
  {
    if (record.GetArchetype() != _archetype)
    {
      throw new ArgumentException("Record archetype must match");
    }
    
    _entityList.Add(record.GetEntityId());
    _entityToRecord.Add(record.GetEntityId(), record);
    _recordList.Add(record);
  }

  internal List<EntityId> GetEntities()
  {
    return _entityList;
  }

  internal ArchetypeDataRecord GetRecord(EntityId entityId)
  {
    return _entityToRecord[entityId];
  }
  
  internal bool Contains(EntityId entityId)
  {
    return _entityToRecord.ContainsKey(entityId);
  }
  
  internal ArchetypeDataRecord RemoveRecord(EntityId entityId)
  {
    ArchetypeDataRecord dataRecord = _entityToRecord[entityId];
    _entityToRecord.Remove(entityId);
    _entityList.Remove(entityId);
    _recordList.Remove(dataRecord);
    return dataRecord;
  }
  
  internal int ExtractAllRecords(List<ArchetypeDataRecord> fillWithExtractedRecords)
  {
    int removed = _recordList.Count;

    fillWithExtractedRecords.Clear();
    fillWithExtractedRecords.AddRange(_recordList);

    _entityToRecord.Clear();
    _entityList.Clear();
    _recordList.Clear();

    return removed;
  }

  internal void PopulateResults(IQueryResult results)
  {
    foreach (var r in _recordList)
    {
      results.AddRecord(r);
    }
  }
}
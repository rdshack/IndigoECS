using ecs;

namespace ecs;


public class FrameInputData : IFrameInputData
{
  public int FrameNum { get; set; }

  private List<AliasId>                             _aliasIds           = new List<AliasId>();
  private Dictionary<ComponentGroup, AliasId>       _groupToAliasLookup = new Dictionary<ComponentGroup, AliasId>();
  private Dictionary<AliasId, List<ComponentGroup>> _inputsByAlias      = new Dictionary<AliasId, List<ComponentGroup>>();
  
  public  IComponentFactory    ComponentPool { get; private set; }
  private IAliasLookup         _aliasLookup;
  private IComponentDefinitions _definitions;

  public void Setup(IComponentFactory pool, IAliasLookup aliasLookup, IComponentDefinitions componentDefinitions)
  {
    ComponentPool = pool;
    _aliasLookup = aliasLookup;
    _definitions = componentDefinitions;
  }

  public void AddComponentGroup(ComponentGroup inputComponentGroup)
  {
    var aliasId = _aliasLookup.GetAliasForArchetype(inputComponentGroup.GetArchetype());
    _groupToAliasLookup.Add(inputComponentGroup, aliasId);
    
    if (!_inputsByAlias.TryGetValue(aliasId, out List<ComponentGroup> groups))
    {
      groups = new List<ComponentGroup>();
      _inputsByAlias.Add(aliasId, groups);
      
      _aliasIds.Add(aliasId);
      _aliasIds.Sort();
    }

    groups.Add(inputComponentGroup);
    groups.Sort(SortByInputKey);
  }

  private int SortByInputKey(ComponentGroup x, ComponentGroup y)
  {
    AliasId xId = _groupToAliasLookup[x];
    AliasId yId = _groupToAliasLookup[y];

    var xComp = x.GetComponent(_aliasLookup.GetInputAliasKeyComponent(xId));
    var yComp = y.GetComponent(_aliasLookup.GetInputAliasKeyComponent(yId));

    return _definitions.CompareComponentFieldKeys(xComp, yComp);
  }

  public IEnumerable<IComponentGroup> GetComponentGroups()
  {
    for (int i = 0; i < _aliasIds.Count; i++)
    {
      var curAliasGroups = _inputsByAlias[_aliasIds[i]];
      foreach (var group in curAliasGroups)
      {
        yield return group;
      }
    }
  }

  public int GetFrameNum()
  {
    return FrameNum;
  }

  public void Reset()
  {
    foreach (var id in _aliasIds)
    {
      var groupsForAlias = _inputsByAlias[id];
      foreach (var group in groupsForAlias)
      {
        group.Reset();
        ComponentPool.ReturnEntityData(group);
      }
    }

    _aliasIds.Clear();
    _inputsByAlias.Clear();
    _groupToAliasLookup.Clear();
    FrameNum = -1;
  }

  public static void Reset(FrameInputData obj)
  {
    obj.Reset();
  }

  public static void CopyTo(FrameInputData       source, 
                            FrameInputData       target,
                            IComponentFactory    copier,
                            ArchetypeGraph       archetypeGraph,
                            IComponentDefinitions definitions)
  {
    target.Reset();
    target.FrameNum = source.FrameNum;
    
    foreach (var compGroup in source.GetComponentGroups())
    {
      var pool = target.ComponentPool;
      var compGroupCopy = pool.GetComponentGroup(archetypeGraph, definitions);

      foreach (var cIdx in archetypeGraph.GetComponentIndicesForArchetype(compGroup.GetArchetype()))
      {
        var toCopy = compGroup.GetComponent(cIdx);
        var copyTarget = pool.Get(cIdx);
        copier.Copy(toCopy, copyTarget);
        compGroupCopy.AddComponentType(cIdx, copyTarget);
      }
            
      target.AddComponentGroup(compGroupCopy);
    }
  }
  
  public static void CopyTo(IDeserializedFrameSyncStore source, 
                            FrameInputData              target,
                            IComponentFactory           copier,
                            ArchetypeGraph              archetypeGraph,
                            IComponentDefinitions        definitions)
  {
    target.Reset();
    target.FrameNum = source.FrameNum;
    
    foreach (var compGroup in source.GetClientInputData())
    {
      var pool = target.ComponentPool;
      var compGroupCopy = pool.GetComponentGroup(archetypeGraph, definitions);

      foreach (var cIdx in archetypeGraph.GetComponentIndicesForArchetype(compGroup.GetArchetype()))
      {
        var toCopy = compGroup.GetComponent(cIdx);
        var copyTarget = pool.Get(cIdx);
        copier.Copy(toCopy, copyTarget);
        compGroupCopy.AddComponentType(cIdx, copyTarget);
      }
            
      target.AddComponentGroup(compGroupCopy);
    }
  }
  
  public static void CopyTo(IDeserializedFrameDataStore source, 
                            FrameInputData              target,
                            IComponentFactory           copier,
                            ArchetypeGraph              archetypeGraph,
                            IComponentDefinitions        definitions)
  {
    target.Reset();
    target.FrameNum = source.FrameNum;
    
    foreach (var compGroup in source.GetEntitiesData())
    {
      var pool = target.ComponentPool;
      var compGroupCopy = pool.GetComponentGroup(archetypeGraph, definitions);

      foreach (var cIdx in archetypeGraph.GetComponentIndicesForArchetype(compGroup.GetArchetype()))
      {
        var toCopy = compGroup.GetComponent(cIdx);
        var copyTarget = pool.Get(cIdx);
        copier.Copy(toCopy, copyTarget);
        compGroupCopy.AddComponentType(cIdx, copyTarget);
      }
            
      target.AddComponentGroup(compGroupCopy);
    }
  }
}
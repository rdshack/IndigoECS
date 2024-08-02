namespace ecs;

public static class FrameInputDataHelpers
{
  public static void CopyTo(IFrameData                  source, 
                            IDeserializedFrameDataStore target,
                            IComponentFactory           copier,
                            ArchetypeGraph              archetypeGraph,
                            IComponentDefinitions       definitions)
  {
    target.Reset();
    target.FrameNum = source.GetFrameNum();
    
    foreach (var compGroup in source.GetEntityRepo().GetEntitiesData())
    {
      var pool = target.ComponentPool;
      foreach (var cIdx in archetypeGraph.GetComponentIndicesForArchetype(compGroup.GetArchetype()))
      {
        var eId = compGroup.GetEntityId();
        var toCopy = compGroup.GetComponent(cIdx);
        var copyTarget = pool.Get(cIdx);
        copier.Copy(toCopy, copyTarget);
        target.AddComponent(eId, copyTarget);

        if (source.IsNewEntity(eId))
        {
          //target.SetNewEntityHash(eId, );
        }
      }
    }
  }
}
using System.Text;

namespace ecs;

public class World
{
  private IWorldUtilities _worldUtilities;
  private EntityRepo        _entityRepo;
  private ArchetypeGraph    _archetypeGraph;
  private FrameRepo         _frameRepo;
  private Systems           _systems;
  
  public World(IWorldUtilities utilities)
  {
    _worldUtilities = utilities;
    _archetypeGraph = new ArchetypeGraph(utilities.GetComponentIndex(), utilities.GetAliasDefinition());

    _systems = new Systems();
    _entityRepo = new EntityRepo(_archetypeGraph, 
                                 utilities.GetComponentIndex(), 
                                 utilities.BuildComponentFactory(), 
                                 utilities.GetLogger());
    
    _frameRepo = new FrameRepo(this, 
                               _entityRepo, 
                               _archetypeGraph);
  }

  public ArchetypeGraph GetArchetypes()
  {
    return _archetypeGraph;
  }

  public EntityRepo GetEntityRepo()
  {
    return _entityRepo;
  }
  
  public void AddSystem(ISystem system)
  {
    _systems.AddSystem(system);
  }

  public void RestoreToFrame(int targetFrame)
  {
    _frameRepo.RestoreToFrame(targetFrame);
  }

  public void Tick(IFrameInputData input)
  {
    if (_frameRepo.GetNextFrame() != input.GetFrameNum())
    {
      Console.WriteLine("Input passed is for wrong frame");
      return;
    }
    
    var logger = _worldUtilities.GetLogger();
    if (logger.HasFlag(LogFlags.SerializationDetails))
    {
      StringBuilder sb = new StringBuilder();

      sb.AppendLine("----------------");
      sb.AppendLine($"About to tick to create frame '{_frameRepo.GetNextFrame()}', next id is '{_entityRepo.GetNextEntityId().Id}' , with input:");
      foreach (var componentGroup in input.GetComponentGroups())
      {
        foreach (var cTypeIndex in _archetypeGraph.GetComponentIndicesForArchetype(componentGroup.GetArchetype()))
        {
          sb.AppendLine(_entityRepo.GetComponentPool().ToString(componentGroup.GetComponent(cTypeIndex)));
        }
      }
      sb.AppendLine("----------------");
      sb.AppendLine($"This input will be combined with the following state:");
      sb.AppendLine(_entityRepo.GetStateString());
      logger.Log(LogFlags.SerializationDetails, sb.ToString());

      sb.Clear();
    }
    
    _entityRepo.PrepareNextFrame(input);
    _systems.Tick();
    _frameRepo.TakeFrameSnapshot();

    if (logger.HasFlag(LogFlags.SerializationDetails))
    {
      StringBuilder sb = new StringBuilder();
      sb.AppendLine($"After ticking, frame state for '{_frameRepo.GetNextFrame() - 1}':");
      sb.AppendLine($"Hash: {_frameRepo.GetLatestFrameHash()}");
      sb.AppendLine(_frameRepo.GetStateString());
      sb.AppendLine("----------------");
      logger.Log(LogFlags.SerializationDetails, sb.ToString());
    }
    
    _entityRepo.ClearInputEntities();
  }

  public void CloneLatestFrame(IComponentFactory pool, FrameData cloneTarget)
  {
    _frameRepo.CloneLatestFrame(pool, cloneTarget);
  }

  public int GetLatestFrameSerialized(bool backAlign, ref byte[] buffer)
  {
    return _frameRepo.GetLatestFrameSerialized(backAlign, ref buffer);
  }
  
  public void GetLatestFrameSyncSerialized(ref byte[] buffer, IByteArrayResizer resizer)
  {
    _frameRepo.GetLatestFrameSyncSerialized(ref buffer, resizer);
  }
  
  public int GetLatestFrameHash()
  {
    return _frameRepo.GetLatestFrameHash();
  }
  
  public int GetFrameHash(int frameNum)
  {
    return _frameRepo.GetFrameHash(frameNum);
  }

  public int GetNextFrameNum()
  {
    return _frameRepo.GetNextFrame();
  }

  public IComponentDefinitions GetComponentLookup()
  {
    return _worldUtilities.GetComponentIndex();
  }

  public IWorldUtilities GetUtilities()
  {
    return _worldUtilities;
  }
}
namespace ecs;

public class SyncFrameInput : IFrameInputData
{
  private IEnumerable<IComponentGroup> _componentGroups;
  private int                            _frameNum;
  
  public SyncFrameInput(int frameNum, IEnumerable<IComponentGroup> componentGroups)
  {
    _frameNum = frameNum;
    _componentGroups = componentGroups;
  }
  
  public IEnumerable<IComponentGroup> GetComponentGroups()
  {
    return _componentGroups;
  }

  public int GetFrameNum()
  {
    return _frameNum;
  }
}
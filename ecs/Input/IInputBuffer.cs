namespace ecs;

public interface IInputBuffer
{
  bool TryGetInputFrame(int num, out IFrameInputData data);
}
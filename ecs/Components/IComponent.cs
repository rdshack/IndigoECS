namespace ecs;

public interface IComponent
{
  
}

public class Component : IComponent
{
  private        ulong _instanceId;
  private static ulong _nextInstanceId = 1;

  public Component()
  {
    _instanceId = _nextInstanceId++;
  }
}
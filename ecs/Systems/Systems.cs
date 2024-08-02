namespace ecs;

public class Systems
{
  private List<ISystem>   _systems;

  public Systems()
  {
    _systems = new List<ISystem>();
  }
  
  public void AddSystem(ISystem system)
  {
    _systems.Add(system);
  }

  public void Tick()
  {
    foreach (var s in _systems)
    {
      s.Execute();
    }
  }
}
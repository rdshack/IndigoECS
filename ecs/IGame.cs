namespace ecs;

public interface IGame
{
  void          BuildWorld(IWorldLogger logger);
  World         GetWorld();
  void          Tick(IFrameInputData input);
  IGameSettings GetSettings();
}

public interface IGameSettings
{
  double GetMsPerFrame();
}

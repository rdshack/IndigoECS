namespace ecs;

[Flags]
public enum LogFlags
{
  None                 = 0,
  SerializationDetails = 1,
  EntityId             = 1 << 1,
  Motion             = 1 << 2
}

public interface IWorldLogger
{
  LogFlags GetLogFlags();
  bool     HasFlag(LogFlags flag);
  void     Log(LogFlags     categories, string s);
  void     Log(string       s);
}
using System.Buffers;
using CommunityToolkit.HighPerformance;

namespace ecs;

public class ArrayResizer : IByteArrayResizer
{
  private static ArrayResizer _instance;

  public static ArrayResizer Instance
  {
    get
    {
      if (_instance == null)
      {
        _instance = new ArrayResizer();
      }

      return _instance;
    }
  }
  
  public void Resize(ref byte[] array, int size)
  {
    Array.Resize(ref array, size);
  }
}

public class ArrayPoolResizer : IByteArrayResizer
{
  private static ArrayPoolResizer _instance;

  public static ArrayPoolResizer Instance
  {
    get
    {
      if (_instance == null)
      {
        _instance = new ArrayPoolResizer();
      }

      return _instance;
    }
  }
  
  public void Resize(ref byte[] array, int size)
  {
    ArrayPool<byte>.Shared.Resize(ref array, size);
  }
}
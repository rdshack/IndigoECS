
using ecs;

public class ObjPool<T>
{
  private class CallLog
  {
    public static int NextId = 0;
    public static DateTime RefTime = DateTime.UtcNow;
    
    public readonly int Id;
    public readonly double MsSincePoolCreation;
    public readonly string RequestStackTrace;

    public CallLog()
    {
      Id = NextId++;
      MsSincePoolCreation = (DateTime.UtcNow - RefTime).TotalMilliseconds;
      RequestStackTrace = System.Environment.StackTrace;
    }
  }
  
  private const int DEFAULT_POOL_SIZE = 5;
  private const int ALERT_SIZE = 700;

  private static int _nextInternalId = 1;

  private int                   _internalId;
  private Stack<T>              _pool;
  private NonAllocLinkedList<T> _inUse;
  private int                   _size;
  private Func<T>               _buildAction;
  private Action<T>             _resetAction;
  private IWorldLogger          _logger;
  private bool                  _useLogs;
  
  private Dictionary<T, CallLog> _requestLogs = new Dictionary<T, CallLog>();
  private Dictionary<T, CallLog> _returnLogs = new Dictionary<T, CallLog>();
  
  public ObjPool(Func<T> builder, Action<T> resetter, IWorldLogger logger = null)
  {
    _internalId = _nextInternalId++;
    _logger = logger;
    _pool = new Stack<T>();
    _inUse = new NonAllocLinkedList<T>();
    _buildAction = builder;
    _resetAction = resetter;
    _size = DEFAULT_POOL_SIZE;
    
    for (int i = 0; i < _size; i++)
    {
      _pool.Push(Build()); 
    }

    if (_internalId == 8)
    {
      SetUseLogs();
    }
  }

  public void SetUseLogs()
  {
    _useLogs = true;
  }

  public int GetInternalId()
  {
    return _internalId;
  }

  public T Get()
  {
    if (_inUse.Count > ALERT_SIZE)
    {
      throw new Exception();
    }
    
    T component;
    if (_pool.Count != 0)
    {
      component = _pool.Pop();
      _inUse.Add(component);

#if OBJ_POOL_DEBUG
      if(_useLogs)
      {
        _requestLogs.Add(component, new CallLog());
        _returnLogs.Remove(component);
      }
#endif
      
      return component;
    }
    
    for (int i = 0; i < _size; i++)
    {
      _pool.Push(Build());

#if OBJ_POOL_DEBUG
      if(_useLogs)
      {
        if (_pool.Count > ALERT_SIZE)
        {
          throw new Exception("obj pool leak");
        }
      }
#endif
    }

    _size *= 2;
    
    component = _pool.Pop();
    _inUse.Add(component);
    
#if OBJ_POOL_DEBUG
      if(_useLogs)
      {
        _requestLogs.Add(component, new CallLog());
      }
#endif
    
    return component;
  }

  public void Return(T component)
  {
#if OBJ_POOL_DEBUG
      if(_useLogs)
      {
        _requestLogs.Remove(component);
      }
#endif
    
    if (_inUse.Remove(component))
    {
#if OBJ_POOL_DEBUG
      if(_useLogs)
      {
        _returnLogs.Add(component, new CallLog());
      }
#endif
      
      _resetAction(component);
      _pool.Push(component);
    }
    else
    {
      if (_returnLogs.TryGetValue(component, out CallLog log))
      {
        
      }
      
      throw new ArgumentException();      
    }
  }

  private T Build()
  {
    return _buildAction();
  }
  
  public void ReturnAll()
  {
    for (int i = _inUse.Count - 1; i >= 0; i--)
    {
      Return(_inUse[i]);
    }
  }
}
using System.Buffers;
using System.Text;

namespace ecs;

internal class FrameRepo
{
  private const int FRAME_HISTORY = 60;
  private const int KEY_FRAME_INTERVAL = 4;

  private World                 _world;
  private IComponentDefinitions _componentDefinitions;
  private IFrameSerializer      _frameSerializer;
  private IComponentFactory      _componentCopier;
  
  private ArchetypeGraph        _archetypeGraph;
  private EntityRepo            _entityRepo;
  private int                   _nextFrame = 1;
  private List<IFrameData>      _keyFrames = new List<IFrameData>();

  private int _frameHistoryKeyFrameLength;
  private int _frameHistoryKeyFrameStart = 1;
  
  private FrameData? _latestFrame;
  private byte[]    _latestFrameSerialized = new byte[1000];
  private int       _serializedFrameByteCount;
  
  private ObjPool<EntityFrameSnapshot> _frameSnapshotPool;
  private ObjPool<FrameData>           _frameDataPool;
  private ObjPool<FrameSyncData>       _frameSyncDataPool;
  
  private List<FrameSyncData> _syncFrames             = new List<FrameSyncData>();
  private List<FrameSyncData> _syncFramesDisposalList = new List<FrameSyncData>();

  private Dictionary<int, int> _frameHashes = new Dictionary<int, int>();
  
  internal FrameRepo(World world, 
                   EntityRepo entityRepo,
                   ArchetypeGraph        archetypeGraph)
  {
    _world = world;
    _frameSerializer = _world.GetUtilities().GetSerializer();
    _entityRepo = entityRepo;
    _componentDefinitions = _world.GetUtilities().GetComponentIndex();
    _componentCopier = _world.GetUtilities().BuildComponentFactory();
    _archetypeGraph = archetypeGraph;
    
    //_componentCopier.SetDebugFlag(ComponentFactoryDebugFlag.EntityData);

    _frameSnapshotPool = new ObjPool<EntityFrameSnapshot>(EntityFrameSnapshot.Build, EntityFrameSnapshot.Reset);
    _frameDataPool = new ObjPool<FrameData>(FrameData.Create, FrameData.Reset);
    _frameSyncDataPool = new ObjPool<FrameSyncData>(FrameSyncData.Create, FrameSyncData.Reset);

    _frameHistoryKeyFrameLength = FRAME_HISTORY / KEY_FRAME_INTERVAL;
  }

  internal void RestoreToFrame(int targetFrame)
  {
    if (targetFrame >= _nextFrame)
    {
      throw new Exception("Cannot restore to future...");
    }
    
    if (targetFrame < _frameHistoryKeyFrameStart)
    {
      throw new Exception("Cannot restore beyond our recorded history...");
    }

    IFrameData baseFrame = null;
    
    //1. Remove all keyframes ahead of restore point, and find base key frame
    for (int i = _keyFrames.Count - 1; i >= 0; i--)
    {
      var curFrame = _keyFrames[i];
      int curFrameNum = _keyFrames[i].GetFrameNum();
      
      if (curFrameNum > targetFrame)
      {
        if (curFrame == _latestFrame)
        {
          _latestFrame = null;
        }
        
        RemoveKeyFrame(i);
      }
      else
      {
        bool isKeyFrame = (curFrameNum % KEY_FRAME_INTERVAL) == 1;
        if (isKeyFrame)
        {
          baseFrame = curFrame;
          break;
        }
      }
    }

    List<int> hashesToValidate = new List<int>();
    hashesToValidate.Add(_frameHashes[baseFrame.GetFrameNum()]);
    _syncFramesDisposalList.Clear();

    //2. Remove all sync/hash data ahead of base frame, and prep list of input frames to apply
    //on top of our base frame.
    int baseFrameNum = baseFrame.GetFrameNum();
    List<IFrameSyncData> syncDatas = new List<IFrameSyncData>();
    for (int i = _syncFrames.Count - 1; i >= 0 && _syncFrames[i].GetFrameNum() > baseFrameNum; i--)
    {
      var frameNum = _syncFrames[i].GetFrameNum();

      if (frameNum <= targetFrame)
      {
        hashesToValidate.Insert(1, _frameHashes[frameNum]);

        if (frameNum != baseFrameNum)
        {
          syncDatas.Insert(0, _syncFrames[i]); 
        }
      }
    }
    
    for (int i = _syncFrames.Count - 1; i >= 0 && _syncFrames[i].GetFrameNum() >= baseFrameNum; i--)
    {
      var frameNum = _syncFrames[i].GetFrameNum();
      if (frameNum >= baseFrameNum)
      {
        _syncFramesDisposalList.Add(_syncFrames[i]);
        _syncFrames.RemoveAt(i); 
      }
    }

    for (int i = _nextFrame - 1; i >= baseFrame.GetFrameNum(); i--)
    {
      _frameHashes.Remove(i);
    }

    _nextFrame = baseFrameNum;
    _entityRepo.ClearAndCopy(baseFrame.GetEntityRepo());
    
    _world.GetUtilities().GetLogger().Log(LogFlags.SerializationDetails, $"Restored to frame '{baseFrame.GetFrameNum()}' with new next id of '{baseFrame.GetEntityRepo().GetNextEntityId().Id}'");
    
    RemoveKeyFrame(0);
    TakeFrameSnapshot();

    int frameHash = _frameHashes[_nextFrame - 1];
    if (frameHash != hashesToValidate[0])
    {
      _world.GetUtilities().GetLogger().Log(LogFlags.SerializationDetails, $"Base frame check failed on restore for frame '{_nextFrame - 1}'");

      throw new Exception();
    }
    
    hashesToValidate.RemoveAt(0);
    _entityRepo.ClearInputEntities();

    foreach (var syncData in syncDatas)
    {
      SyncFrameInput syncFrameInput = new SyncFrameInput(syncData.GetFrameNum(), syncData.GetClientInputData());
      _world.GetUtilities().GetLogger().Log(LogFlags.SerializationDetails, $"About to snap tick sync frame '{_nextFrame - 1}'");
      _world.Tick(syncFrameInput);
      
      frameHash = _frameHashes[_nextFrame - 1];
      if (frameHash != hashesToValidate[0])
      {
        _world.GetUtilities().GetLogger().Log(LogFlags.SerializationDetails, $"Sync frame check failed on restore for frame '{_nextFrame - 1}'");
        throw new Exception();
      }
      
      hashesToValidate.RemoveAt(0);
    }

    foreach (var toDispose in _syncFramesDisposalList)
    {
      _frameSyncDataPool.Return(toDispose);
    }
  }
  
  internal string GetStateString()
  {
    StringBuilder sb = new StringBuilder();
    var frameData = _latestFrame;
    sb.AppendLine($"Frame state for '{frameData.GetFrameNum()}'");
    foreach (var e in frameData.GetEntityRepo().GetEntitiesData())
    {
      sb.AppendLine("----------------");
      sb.AppendLine($"Entity '{e.GetEntityId().Id}':");
      foreach (var cTypeIndex in _archetypeGraph.GetComponentIndicesForArchetype(e.GetArchetype()))
      {
        sb.AppendLine(_componentCopier.ToString(e.GetComponent(cTypeIndex)));
      }
      
      sb.AppendLine("----------------");
    }

    return sb.ToString();
  }

  internal void TakeFrameSnapshot()
  {
    int frame = _nextFrame++;

    FrameData frameData = _frameDataPool.Get();
    frameData.Init(frame, _entityRepo.GetNextEntityId(), _entityRepo);
    
    _serializedFrameByteCount = _frameSerializer.Serialize(_archetypeGraph, ArrayResizer.Instance, frameData, ref _latestFrameSerialized);
    int hash = _frameSerializer.CreateStateHash(_latestFrameSerialized, 
                                                0, 
                                                _serializedFrameByteCount);
    _frameHashes.Add(frame, hash);
    frameData.Clear();
    _frameDataPool.Return(frameData);
    
    //_world.GetUtilities().GetLogger().Log(LogFlags.SerializationDetails, $"Storing frame hash for '{frame}': hash-{hash}, nextId-{_entityRepo.GetNextEntityId().Id}");

    EntityFrameSnapshot cloneTarget = _frameSnapshotPool.Get();
    cloneTarget.Init(_componentCopier, _componentDefinitions, _archetypeGraph);
    _entityRepo.CloneEntities(cloneTarget, frame, _componentCopier, CloneType.All);
    
    
    FrameData fullClone = _frameDataPool.Get();
    fullClone.Init(frame, _entityRepo.GetNextEntityId(), cloneTarget);
    
    //For key frames, also store deep copy of all entity data
    bool isKeyFrame = (frame % KEY_FRAME_INTERVAL) == 1;
    if (isKeyFrame)
    {
      _keyFrames.Add(fullClone);
    }
    
    //Always store input
    EntityFrameSnapshot inputCloneTarget = _frameSnapshotPool.Get();
    cloneTarget.Init(_componentCopier, _componentDefinitions, _archetypeGraph);
    _entityRepo.CloneEntities(inputCloneTarget, frame, _componentCopier, CloneType.InputOnly);

    FrameSyncData frameSyncData = _frameSyncDataPool.Get();
    frameSyncData.Init(frame, hash, inputCloneTarget.GetEntitiesData(), _componentCopier);
    _syncFrames.Add(frameSyncData);
    
    inputCloneTarget.ClearEntityList();
    _frameSnapshotPool.Return(inputCloneTarget);

    //dispose last frame, unless its a key frame (in which it case it will be disposed later).
    bool lastFrameWasKeyFrame = ((frame - 1) % KEY_FRAME_INTERVAL) == 1;
    if (!lastFrameWasKeyFrame)
    {
      if (_latestFrame != null)
      {
        _frameSnapshotPool.Return((EntityFrameSnapshot)_latestFrame.GetEntityRepo());
        _frameDataPool.Return(_latestFrame);
      }
    }
    
    _latestFrame = fullClone;
    
    //clean up old data if we need to vacate a key frame (and all sync frames / hashes after, until next key frame)
    if (isKeyFrame)
    {
      int keyFrameIndex = frame / KEY_FRAME_INTERVAL;
      int newKeyFrameHistoryStart = Math.Max(1, keyFrameIndex - _frameHistoryKeyFrameLength);
      if (newKeyFrameHistoryStart > _frameHistoryKeyFrameStart)
      {
        for (int i = _frameHistoryKeyFrameStart * KEY_FRAME_INTERVAL; i >= _frameHistoryKeyFrameStart; i--)
        {
          _frameHashes.Remove(i);
        }

        for (int i = KEY_FRAME_INTERVAL - 1; i >= 0; i--)
        {
          _frameSyncDataPool.Return(_syncFrames[i]);
          _syncFrames.RemoveAt(i);
        }
        
        RemoveKeyFrame(0);

        _frameHistoryKeyFrameStart = newKeyFrameHistoryStart;
      } 
    }
  }

  private void RemoveKeyFrame(int i)
  {
    _frameSnapshotPool.Return((EntityFrameSnapshot) _keyFrames[i].GetEntityRepo());

    FrameData frameData = (FrameData) _keyFrames[i];
    frameData.Clear();
    _frameDataPool.Return(frameData);
    _keyFrames.RemoveAt(i);
  }

  internal int GetNextFrame()
  {
    return _nextFrame;
  }

  public void CloneLatestFrame(IComponentFactory pool, FrameData cloneTarget)
  {
    _latestFrame.Clone(pool, _archetypeGraph, _componentDefinitions, cloneTarget);
  }

  public int GetLatestFrameHash()
  {
    return _frameHashes[_latestFrame.GetFrameNum()];
  }
  
  public int GetFrameHash(int frameNum)
  {
    return _frameHashes[frameNum];
  }

  public int GetLatestFrameSerialized(bool backAlign, ref byte[] buffer)
  {
    if (buffer.Length < _serializedFrameByteCount)
    {
      Array.Resize(ref buffer, _serializedFrameByteCount * 2);
    }

    if (backAlign)
    {
      int offset = buffer.Length - _serializedFrameByteCount;
      Array.Copy(_latestFrameSerialized, 0, buffer, offset, _serializedFrameByteCount);
    }
    else
    {
      Array.Copy(_latestFrameSerialized, buffer, _serializedFrameByteCount);
    }

    return _serializedFrameByteCount;
  }

  public void GetLatestFrameSyncSerialized(ref byte[] buffer, IByteArrayResizer resizer)
  {
    int latestSyncIndex = _syncFrames.Count - 1;
    _frameSerializer.Serialize(_archetypeGraph, resizer, _syncFrames[latestSyncIndex], ref buffer);
  }
}
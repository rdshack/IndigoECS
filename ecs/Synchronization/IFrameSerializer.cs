namespace ecs;

public interface IByteArrayResizer
{
  void Resize(ref byte[] array, int size);
}

public interface IFrameSerializer
{
  void DeserializeSyncFrame(byte[]  data,           IDeserializedFrameSyncStore output, int        dataStart);
  void DeserializeFrame(byte[]      input,          IDeserializedFrameDataStore output, int        dataStart);
  void DeserializeInputFrame(byte[] input,          IDeserializedFrameSyncStore output, int        dataStart);
  int Serialize(ArchetypeGraph archetypeGraph, IByteArrayResizer resizer, IFrameData data,   ref byte[] resultBuffer);
  int Serialize(ArchetypeGraph archetypeGraph, IByteArrayResizer resizer,  IFrameSyncData data,   ref byte[] resultBuffer);
  int Serialize(ArchetypeGraph  archetypeGraph, IByteArrayResizer resizer,  IFrameInputData data,   ref byte[] resultBuffer);
  int CreateStateHash(byte[] seg, int pos, int len);
}
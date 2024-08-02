namespace ecs;

[System.AttributeUsage(System.AttributeTargets.Class)]
public class SingletonComponent : System.Attribute
{
}

[System.AttributeUsage(System.AttributeTargets.Class)]
public class SingleFrameComponent : System.Attribute
{
}

[System.AttributeUsage(System.AttributeTargets.Field)]
public class KeyComponentField : System.Attribute
{
}
public abstract class LuaInstanceClass
{
    public abstract string ClassName { get; }
    public virtual void Initialize(LuaInstance instance) { }
}

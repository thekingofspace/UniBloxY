using MoonSharp.Interpreter;

public abstract class LuaInstanceClass
{
    public abstract string ClassName { get; }

    public virtual void Initialize(LuaInstance instance) { }

    public virtual void OnEnterScene(LuaInstance instance) { }
    public virtual void OnExitScene(LuaInstance instance) { }

    public virtual void OnChildAdded(LuaInstance instance, LuaInstance child) { }
    public virtual void OnChildRemoved(LuaInstance instance, LuaInstance child) { }

    public virtual bool TryGetProperty(LuaInstance instance, string key, out DynValue value)
    {
        value = DynValue.Nil;
        return false;
    }

    public virtual bool TrySetProperty(LuaInstance instance, string key, DynValue value)
    {
        return false;
    }
}

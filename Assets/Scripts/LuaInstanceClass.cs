using MoonSharp.Interpreter;
using UnityEngine;

public abstract class LuaInstanceClass
{
    public abstract string ClassName { get; }

    public virtual void Initialize(LuaInstance instance) { }

    public virtual bool ParentsUnityObject => true;

    public virtual bool Clonable => false;

    public virtual void CopyState(LuaInstance source, LuaInstance target) { }

    public virtual void ImportFromUnityObject(LuaInstance instance, GameObject go) { }

    public virtual void OnEnterScene(LuaInstance instance) { }
    public virtual void OnExitScene(LuaInstance instance) { }

    public virtual void OnChildAdded(LuaInstance instance, LuaInstance child) { }
    public virtual void OnChildRemoved(LuaInstance instance, LuaInstance child) { }

    public virtual void OnAncestryChanged(LuaInstance instance) { }

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

using MoonSharp.Interpreter;
using UnityEngine;

public abstract class LuaInstanceClass
{
    public abstract string ClassName { get; }

    public virtual void Initialize(LuaInstance instance) { }

    public virtual bool ParentsUnityObject => true;

    // Whether instances of this class can be cloned. Most instances are
    // *not* cloneable by default (e.g. Camera, DataModel, lights);
    // BaseCube / Folder / RenderGroup opt in.
    public virtual bool Clonable => false;

    // Copy class-managed state (UserState, properties) from source → target.
    // Default implementation re-runs Initialize and replays known properties
    // via TrySetProperty; override for classes that store data outside that path.
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

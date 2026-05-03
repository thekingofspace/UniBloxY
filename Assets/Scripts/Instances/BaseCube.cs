using MoonSharp.Interpreter;
using UnityEngine;

public class BaseCube : Renderable
{
    public override string ClassName => "BaseCube";

    public override bool ParentsUnityObject => false;

    private class State
    {
        public LuaVector3 Size = LuaVector3.One;
        public LuaCFrame CFrame = LuaCFrame.Identity;
    }

    public override void Initialize(LuaInstance instance)
    {
        instance.UserState = new State();
        instance.Moveable = true;
    }

    public override void ImportFromUnityObject(LuaInstance instance, GameObject go)
    {
        var s = (State)instance.UserState;
        var t = go.transform;
        var pos = t.position;
        var rot = t.eulerAngles;
        var scale = t.localScale;
        s.Size = new LuaVector3(scale.x, scale.y, scale.z);
        s.CFrame = new LuaCFrame(
            new LuaVector3(pos.x, pos.y, pos.z),
            new LuaVector3(rot.x, rot.y, rot.z));
        SetRender(instance, true);
    }

    public override bool TryGetProperty(LuaInstance instance, string key, out DynValue value)
    {
        var s = (State)instance.UserState;
        switch (key)
        {
            case "Size": value = UserData.Create(s.Size); return true;
            case "CFrame": value = UserData.Create(s.CFrame); return true;
        }
        return base.TryGetProperty(instance, key, out value);
    }

    public override bool TrySetProperty(LuaInstance instance, string key, DynValue value)
    {
        var s = (State)instance.UserState;
        switch (key)
        {
            case "Size":
                if (value.Type == DataType.UserData && value.UserData.Object is LuaVector3 v)
                {
                    s.Size = v;
                    ApplyTransform(instance, s);
                    return true;
                }
                throw new ScriptRuntimeException("BaseCube.Size must be a Vector3");
            case "CFrame":
                if (value.Type == DataType.UserData && value.UserData.Object is LuaCFrame cf)
                {
                    var old = s.CFrame;
                    s.CFrame = cf;
                    ApplyTransform(instance, s);
                    PropagateMoveToDescendants(instance, old, cf);
                    return true;
                }
                throw new ScriptRuntimeException("BaseCube.CFrame must be a CFrame");
        }

        return base.TrySetProperty(instance, key, value);
    }

    protected override void OnRenderStateChanged(LuaInstance instance)
    {
        SyncRender(instance, (State)instance.UserState);
    }

    public override void OnEnterScene(LuaInstance instance)
    {
        SyncRender(instance, (State)instance.UserState);
    }

    public override void OnExitScene(LuaInstance instance)
    {
        DestroyUnityObject(instance);
    }

    private void SyncRender(LuaInstance instance, State s)
    {
        if (EffectiveRender(instance))
        {
            if (instance.UnityObject == null)
                CreateCube(instance, s);
        }
        else
        {
            DestroyUnityObject(instance);
        }
    }

    private static void CreateCube(LuaInstance instance, State s)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = instance.Name;
        instance.UnityObject = go;
        ApplyTransform(instance, s);
    }

    private static void DestroyUnityObject(LuaInstance instance)
    {
        if (instance.UnityObject != null)
        {
            Object.Destroy(instance.UnityObject);
            instance.UnityObject = null;
        }
    }

    private static void ApplyTransform(LuaInstance instance, State s)
    {
        var go = instance.UnityObject;
        if (go == null) return;
        var p = s.CFrame.Position;
        var r = s.CFrame.Rotation;
        go.transform.position = new Vector3(p.X, p.Y, p.Z);
        go.transform.eulerAngles = new Vector3(r.X, r.Y, r.Z);
        go.transform.localScale = new Vector3(s.Size.X, s.Size.Y, s.Size.Z);
    }

    private static void PropagateMoveToDescendants(LuaInstance instance, LuaCFrame oldCF, LuaCFrame newCF)
    {
        var delta = newCF * oldCF.Inverse();
        for (int i = 0; i < instance.Children.Count; i++)
            ApplyDeltaRecursive(instance.Children[i], delta);
    }

    private static void ApplyDeltaRecursive(LuaInstance node, LuaCFrame delta)
    {
        if (node.Moveable && node.ClassDef is BaseCube cubeDef && node.UserState is State cs)
        {
            cs.CFrame = delta * cs.CFrame;
            ApplyTransform(node, cs);
            node.FirePropertyChanged("CFrame");
        }
        for (int i = 0; i < node.Children.Count; i++)
            ApplyDeltaRecursive(node.Children[i], delta);
    }
}

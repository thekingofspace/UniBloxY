using System.Runtime.CompilerServices;
using MoonSharp.Interpreter;
using UnityEngine;

public class MeshPart : BasePart
{
    public override string ClassName => "MeshPart";

    private class MeshData
    {
        public LuaMesh Model;
        public LuaSkeleton Skeleton;
        public LuaAnimator Animator;
    }

    private static readonly ConditionalWeakTable<LuaInstance, MeshData> meshData = new();
    private static MeshData GetMeshData(LuaInstance instance) =>
        meshData.GetValue(instance, _ => new MeshData());

    public override void CopyState(LuaInstance source, LuaInstance target)
    {
        base.CopyState(source, target);
        var src = GetMeshData(source);
        var dst = GetMeshData(target);
        dst.Model = src.Model;
        dst.Skeleton = src.Skeleton;
    }

    protected override GameObject BuildGameObject(LuaInstance instance, State s)
    {
        var d = GetMeshData(instance);

        if (d.Skeleton != null && d.Skeleton.Prefab != null)
        {
            var go = Object.Instantiate(d.Skeleton.Prefab);
            if (d.Model != null && d.Model.Mesh != null)
            {
                var smr = go.GetComponentInChildren<SkinnedMeshRenderer>();
                if (smr != null) smr.sharedMesh = d.Model.Mesh;
            }
            return go;
        }

        var emptyGo = new GameObject("MeshPart");
        var mf = emptyGo.AddComponent<MeshFilter>();
        emptyGo.AddComponent<MeshRenderer>();
        if (d.Model != null) mf.sharedMesh = d.Model.Mesh;
        return emptyGo;
    }

    protected override void OnUnityObjectCreated(LuaInstance instance, GameObject go)
    {
        var d = GetMeshData(instance);
        if (d.Skeleton != null) return;
        base.OnUnityObjectCreated(instance, go);
    }

    public override bool TryGetProperty(LuaInstance instance, string key, out DynValue value)
    {
        var d = GetMeshData(instance);
        switch (key)
        {
            case "Model":
                value = d.Model != null ? UserData.Create(d.Model) : DynValue.Nil;
                return true;
            case "Skeleton":
                value = d.Skeleton != null ? UserData.Create(d.Skeleton) : DynValue.Nil;
                return true;
            case "Animator":
                value = d.Animator != null ? UserData.Create(d.Animator) : DynValue.Nil;
                return true;
            case "LinkAnimator":
                value = DynValue.NewCallback((ctx, args) =>
                {
                    var animator = LinkAnimator(instance);
                    return animator != null ? UserData.Create(animator) : DynValue.Nil;
                });
                return true;
        }
        return base.TryGetProperty(instance, key, out value);
    }

    public override bool TrySetProperty(LuaInstance instance, string key, DynValue value)
    {
        var d = GetMeshData(instance);
        switch (key)
        {
            case "Model":
                d.Model = ResolveModel(value);
                Rebuild(instance);
                return true;
            case "Skeleton":
                d.Skeleton = ResolveSkeleton(value);
                Rebuild(instance);
                return true;
            case "Animator":
                throw new ScriptRuntimeException("MeshPart.Animator is read-only — use :LinkAnimator() to create one");
        }
        return base.TrySetProperty(instance, key, value);
    }

    public override void OnExitScene(LuaInstance instance)
    {
        base.OnExitScene(instance);
        var d = GetMeshData(instance);
        d.Animator = null;
    }

    private static LuaMesh ResolveModel(DynValue value)
    {
        if (value == null || value.IsNil()) return null;
        if (value.Type == DataType.UserData)
        {
            if (value.UserData.Object is LuaMesh m) return m;
            if (value.UserData.Object is LuaSkeleton)
                throw new ScriptRuntimeException("MeshPart.Model: pass a Skeleton via the Skeleton property");
        }
        if (value.Type == DataType.String)
        {
            if (AssetService.Instance == null)
                throw new ScriptRuntimeException("MeshPart.Model: AssetService not available");
            return AssetService.Instance.GetMesh(value.String);
        }
        throw new ScriptRuntimeException("MeshPart.Model must be a Mesh, a name string, or nil");
    }

    private static LuaSkeleton ResolveSkeleton(DynValue value)
    {
        if (value == null || value.IsNil()) return null;
        if (value.Type == DataType.UserData && value.UserData.Object is LuaSkeleton s) return s;
        if (value.Type == DataType.String)
        {
            if (AnimatorService.Instance == null)
                throw new ScriptRuntimeException("MeshPart.Skeleton: AnimatorService not available");
            return AnimatorService.Instance.ImportSkeleton(value.String);
        }
        throw new ScriptRuntimeException("MeshPart.Skeleton must be a Skeleton, a name string, or nil");
    }

    private static void Rebuild(LuaInstance instance)
    {
        var d = GetMeshData(instance);
        d.Animator = null;

        if (instance.UnityObject != null)
        {
            Object.Destroy(instance.UnityObject);
            instance.UnityObject = null;
        }

        RefreshRenderSubtree(instance);
    }

    private static LuaAnimator LinkAnimator(LuaInstance instance)
    {
        var d = GetMeshData(instance);
        if (d.Animator != null && d.Animator.Component != null) return d.Animator;

        if (instance.UnityObject == null)
            RefreshRenderSubtree(instance);

        var go = instance.UnityObject;
        if (go == null)
            throw new ScriptRuntimeException(
                "MeshPart:LinkAnimator() — no GameObject yet (set Render=true and parent the part to game)");

        var comp = go.GetComponent<Animation>();
        if (comp == null) comp = go.AddComponent<Animation>();

        d.Animator = new LuaAnimator(instance, comp);
        return d.Animator;
    }
}

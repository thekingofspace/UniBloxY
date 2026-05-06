using MoonSharp.Interpreter;
using UnityEngine;

public class CachedModel : Renderable
{
    public override string ClassName => "CachedModel";
    public override bool ParentsUnityObject => false;
    public override bool Clonable => true;

    private class State
    {
        public LuaVector3 Size = LuaVector3.One;
        public LuaCFrame CFrame = LuaCFrame.Identity;
        public LuaColor3 Color = new LuaColor3(1f, 1f, 1f);
        public float Transparency = 0f;
        public LuaMesh Model;
        public LuaMaterial Material;
        public LuaTexture Texture;
        // One-shot Object.Instantiate source captured on Clone via CopyState.
        // CreatePart pops it and instantiates from it instead of building a
        // fresh GameObject + MeshFilter + MeshRenderer.
        public GameObject Template;
    }

    public override void Initialize(LuaInstance instance)
    {
        instance.UserState = new State();
        instance.Moveable = true;
    }

    public override void CopyState(LuaInstance source, LuaInstance target)
    {
        base.CopyState(source, target);
        if (source.UserState is State src && target.UserState is State dst)
        {
            dst.Size = src.Size;
            dst.CFrame = src.CFrame;
            dst.Color = src.Color;
            dst.Transparency = src.Transparency;
            dst.Model = src.Model;
            dst.Material = src.Material;
            dst.Texture = src.Texture;
            // Mirror BasePart's clone-template hand-off so CachedModel clones
            // also Object.Instantiate from the source GameObject instead of
            // rebuilding mesh-renderer scaffolding from scratch.
            if (source.UnityObject != null) dst.Template = source.UnityObject;
        }
    }

    public override bool TryGetProperty(LuaInstance instance, string key, out DynValue value)
    {
        var s = (State)instance.UserState;
        switch (key)
        {
            case "Size": value = UserData.Create(s.Size); return true;
            case "CFrame": value = UserData.Create(s.CFrame); return true;
            case "Color":
            case "Color3":
                value = UserData.Create(s.Color); return true;
            case "Transparency":
                value = DynValue.NewNumber(s.Transparency); return true;
            case "Model":
                value = s.Model != null ? UserData.Create(s.Model) : DynValue.Nil; return true;
            case "Material":
                value = s.Material != null ? UserData.Create(s.Material) : DynValue.Nil; return true;
            case "Texture":
                value = s.Texture != null ? UserData.Create(s.Texture) : DynValue.Nil; return true;
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
                throw new ScriptRuntimeException("CachedModel.Size must be a Vector3");
            case "CFrame":
                if (value.Type == DataType.UserData && value.UserData.Object is LuaCFrame cf)
                {
                    s.CFrame = cf;
                    ApplyTransform(instance, s);
                    return true;
                }
                throw new ScriptRuntimeException("CachedModel.CFrame must be a CFrame");
            case "Color":
            case "Color3":
                if (value.Type == DataType.UserData && value.UserData.Object is LuaColor3 c)
                {
                    s.Color = c;
                    ApplyMaterialBlock(instance, s);
                    return true;
                }
                throw new ScriptRuntimeException("CachedModel.Color must be a Color3");
            case "Transparency":
                if (value.Type != DataType.Number)
                    throw new ScriptRuntimeException("CachedModel.Transparency must be a number");
                s.Transparency = Mathf.Clamp01((float)value.Number);
                ApplyMaterialBlock(instance, s);
                return true;
            case "Model":
                s.Model = ResolveMesh(value);
                ApplyMesh(instance, s);
                return true;
            case "Material":
                s.Material = ResolveMaterial(value);
                ApplyMaterial(instance, s);
                ApplyMaterialBlock(instance, s);
                return true;
            case "Texture":
                s.Texture = ResolveTexture(value);
                ApplyMaterialBlock(instance, s);
                return true;
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

    private static void SyncRender(LuaInstance instance, State s)
    {
        if (EffectiveRender(instance))
        {
            if (instance.UnityObject == null) CreatePart(instance, s);
        }
        else
        {
            DestroyUnityObject(instance);
        }
    }

    private static void CreatePart(LuaInstance instance, State s)
    {
        // Pop the one-shot clone template (null if none / source destroyed).
        var template = s.Template;
        s.Template = null;

        GameObject go;
        if (template != null)
        {
            go = Object.Instantiate(template);
            go.name = instance.Name;
        }
        else
        {
            go = new GameObject(instance.Name);
            var mf = go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
            if (s.Model != null) mf.sharedMesh = s.Model.Mesh;
        }
        instance.UnityObject = go;

        ApplyMaterial(instance, s);
        ApplyTransform(instance, s);
        ApplyMaterialBlock(instance, s);
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

    private static void ApplyMesh(LuaInstance instance, State s)
    {
        var go = instance.UnityObject;
        if (go == null) return;
        var mf = go.GetComponent<MeshFilter>();
        if (mf != null) mf.sharedMesh = s.Model?.Mesh;
    }

    private static Material defaultSharedMaterial;
    private static Material GetDefaultSharedMaterial()
    {
        if (defaultSharedMaterial == null)
        {
            var sh = Resources.Load<Shader>("Shaders/Default");
            if (sh != null) defaultSharedMaterial = new Material(sh) { name = "CachedModelDefault" };
        }
        return defaultSharedMaterial;
    }

    private static void ApplyMaterial(LuaInstance instance, State s)
    {
        var go = instance.UnityObject;
        if (go == null) return;
        var mr = go.GetComponent<MeshRenderer>();
        if (mr == null) return;
        mr.sharedMaterial = s.Material?.Source ?? GetDefaultSharedMaterial();
    }

    private static readonly int colorProp = Shader.PropertyToID("_Color");
    private static readonly int baseColorProp = Shader.PropertyToID("_BaseColor");
    private static readonly int mainTexProp = Shader.PropertyToID("_MainTex");
    private static readonly int baseMapProp = Shader.PropertyToID("_BaseMap");
    private static MaterialPropertyBlock scratchBlock;

    private static void ApplyMaterialBlock(LuaInstance instance, State s)
    {
        var go = instance.UnityObject;
        if (go == null) return;
        var mr = go.GetComponent<MeshRenderer>();
        if (mr == null) return;

        scratchBlock ??= new MaterialPropertyBlock();
        mr.GetPropertyBlock(scratchBlock);

        var color = new Color(s.Color.R, s.Color.G, s.Color.B, 1f - s.Transparency);
        scratchBlock.SetColor(colorProp, color);
        scratchBlock.SetColor(baseColorProp, color);

        if (s.Texture != null && s.Texture.Texture != null)
        {
            scratchBlock.SetTexture(mainTexProp, s.Texture.Texture);
            scratchBlock.SetTexture(baseMapProp, s.Texture.Texture);
        }

        mr.SetPropertyBlock(scratchBlock);
    }

    private static LuaMesh ResolveMesh(DynValue value)
    {
        if (value == null || value.IsNil()) return null;
        if (value.Type == DataType.UserData && value.UserData.Object is LuaMesh m) return m;
        if (value.Type == DataType.String)
        {
            if (AssetService.Instance == null)
                throw new ScriptRuntimeException("CachedModel.Model: AssetService not available");
            return AssetService.Instance.GetMesh(value.String);
        }
        throw new ScriptRuntimeException("CachedModel.Model must be a Mesh, name string, or nil");
    }

    private static LuaMaterial ResolveMaterial(DynValue value)
    {
        if (value == null || value.IsNil()) return null;
        if (value.Type == DataType.UserData && value.UserData.Object is LuaMaterial m) return m;
        if (value.Type == DataType.String)
        {
            if (AssetService.Instance == null)
                throw new ScriptRuntimeException("CachedModel.Material: AssetService not available");
            return AssetService.Instance.GetMaterial(value.String);
        }
        throw new ScriptRuntimeException("CachedModel.Material must be a Material, name string, or nil");
    }

    private static LuaTexture ResolveTexture(DynValue value)
    {
        if (value == null || value.IsNil()) return null;
        if (value.Type == DataType.UserData && value.UserData.Object is LuaTexture t) return t;
        if (value.Type == DataType.String)
        {
            if (AssetService.Instance == null)
                throw new ScriptRuntimeException("CachedModel.Texture: AssetService not available");
            return AssetService.Instance.GetTexture(value.String);
        }
        throw new ScriptRuntimeException("CachedModel.Texture must be a Texture, name string, or nil");
    }
}

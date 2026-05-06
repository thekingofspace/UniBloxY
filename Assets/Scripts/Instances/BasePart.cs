using MoonSharp.Interpreter;
using UnityEngine;

public class BasePart : Shadable
{
    public override string ClassName => "BasePart";

    public override bool ParentsUnityObject => false;

    public override bool Clonable => true;

    public override void CopyState(LuaInstance source, LuaInstance target)
    {
        base.CopyState(source, target);
        if (source.UserState is State src && target.UserState is State dst)
        {
            dst.Size = src.Size;
            dst.CFrame = src.CFrame;
            dst.Shape = src.Shape;
            dst.Color = src.Color;
            dst.Transparency = src.Transparency;
        }
    }

    public enum PartShape { Cube, Sphere, Cylinder, Capsule, Plane, Quad }

    private class State
    {
        public LuaVector3 Size = LuaVector3.One;
        public LuaCFrame CFrame = LuaCFrame.Identity;
        public PartShape Shape = PartShape.Cube;
        public LuaColor3 Color = new LuaColor3(1f, 1f, 1f);
        public float Transparency = 0f;
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

        s.Shape = SniffShape(go);

        var renderer = go.GetComponent<Renderer>();
        if (renderer != null && renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_Color"))
        {
            var c = renderer.sharedMaterial.color;
            s.Color = new LuaColor3(c.r, c.g, c.b);
            s.Transparency = 1f - c.a;
        }
        SetRender(instance, true);
    }

    public override bool TryGetProperty(LuaInstance instance, string key, out DynValue value)
    {
        var s = (State)instance.UserState;
        switch (key)
        {
            case "Size": value = UserData.Create(s.Size); return true;
            case "CFrame": value = UserData.Create(s.CFrame); return true;
            case "Shape": value = DynValue.NewString(s.Shape.ToString()); return true;
            case "Color":
            case "Color3":
                value = UserData.Create(s.Color); return true;
            case "Transparency":
                value = DynValue.NewNumber(s.Transparency); return true;
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
                throw new ScriptRuntimeException("BasePart.Size must be a Vector3");
            case "CFrame":
                if (value.Type == DataType.UserData && value.UserData.Object is LuaCFrame cf)
                {
                    var old = s.CFrame;
                    s.CFrame = cf;
                    ApplyTransform(instance, s);
                    PropagateMoveToDescendants(instance, old, cf);
                    return true;
                }
                throw new ScriptRuntimeException("BasePart.CFrame must be a CFrame");
            case "Shape":
                if (value.Type != DataType.String)
                    throw new ScriptRuntimeException("BasePart.Shape must be a string");
                if (!ResolveShape(value.String, out var shape))
                    throw new ScriptRuntimeException(
                        $"BasePart.Shape \"{value.String}\" is not recognized (Cube/Sphere/Cylinder/Capsule/Plane/Quad)");
                if (s.Shape != shape)
                {
                    s.Shape = shape;

                    DestroyUnityObject(instance);
                    SyncRender(instance, s);
                }
                return true;
            case "Color":
            case "Color3":
                if (value.Type == DataType.UserData && value.UserData.Object is LuaColor3 c)
                {
                    s.Color = c;
                    ApplyColor(instance, s);
                    return true;
                }
                throw new ScriptRuntimeException("BasePart.Color must be a Color3");
            case "Transparency":
                if (value.Type != DataType.Number)
                    throw new ScriptRuntimeException("BasePart.Transparency must be a number");
                s.Transparency = Mathf.Clamp01((float)value.Number);
                ApplyColor(instance, s);
                return true;
        }

        return base.TrySetProperty(instance, key, value);
    }

    protected override void OnRenderStateChanged(LuaInstance instance)
    {
        SyncRender(instance, (State)instance.UserState);
        base.OnRenderStateChanged(instance);
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
                CreatePart(instance, s);
        }
        else
        {
            DestroyUnityObject(instance);
        }
    }

    private static Shader defaultShader;
    private static Shader transparentShader;

    private static void CreatePart(LuaInstance instance, State s)
    {
        var go = GameObject.CreatePrimitive(ShapeToPrimitive(s.Shape));
        go.name = instance.Name;

        var renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (defaultShader == null)
                defaultShader = Resources.Load<Shader>("Shaders/Default");
            renderer.sharedMaterial = defaultShader != null ? new Material(defaultShader) : null;
        }

        instance.UnityObject = go;
        ApplyTransform(instance, s);
        ApplyColor(instance, s);
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

    private static void ApplyColor(LuaInstance instance, State s)
    {
        var go = instance.UnityObject;
        if (go == null) return;
        var renderer = go.GetComponent<Renderer>();
        if (renderer == null) return;

        var mats = renderer.sharedMaterials;
        var color = new Color(s.Color.R, s.Color.G, s.Color.B, 1f - s.Transparency);
        for (int i = 0; i < mats.Length; i++)
        {
            if (mats[i] == null) continue;
            if (mats[i].HasProperty("_Color")) mats[i].color = color;
            if (mats[i].HasProperty("_BaseColor")) mats[i].SetColor("_BaseColor", color);

            if (s.Transparency > 0f)
            {
                mats[i].SetFloat("_Mode", 3f);
                if (mats[i].HasProperty("_SrcBlend"))
                    mats[i].SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                if (mats[i].HasProperty("_DstBlend"))
                    mats[i].SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                if (mats[i].HasProperty("_ZWrite"))
                    mats[i].SetInt("_ZWrite", 0);
                mats[i].DisableKeyword("_ALPHATEST_ON");
                mats[i].EnableKeyword("_ALPHABLEND_ON");
                mats[i].DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mats[i].renderQueue = 3000;
            }
            else
            {
                mats[i].SetFloat("_Mode", 0f);
                if (mats[i].HasProperty("_SrcBlend"))
                    mats[i].SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                if (mats[i].HasProperty("_DstBlend"))
                    mats[i].SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                if (mats[i].HasProperty("_ZWrite"))
                    mats[i].SetInt("_ZWrite", 1);
                mats[i].DisableKeyword("_ALPHATEST_ON");
                mats[i].DisableKeyword("_ALPHABLEND_ON");
                mats[i].DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mats[i].renderQueue = -1;
            }
        }
    }

    private static UnityEngine.PrimitiveType ShapeToPrimitive(PartShape shape)
    {
        switch (shape)
        {
            case PartShape.Sphere:   return UnityEngine.PrimitiveType.Sphere;
            case PartShape.Cylinder: return UnityEngine.PrimitiveType.Cylinder;
            case PartShape.Capsule:  return UnityEngine.PrimitiveType.Capsule;
            case PartShape.Plane:    return UnityEngine.PrimitiveType.Plane;
            case PartShape.Quad:     return UnityEngine.PrimitiveType.Quad;
            default:                  return UnityEngine.PrimitiveType.Cube;
        }
    }

    private static bool ResolveShape(string name, out PartShape shape)
    {
        shape = PartShape.Cube;
        if (string.IsNullOrEmpty(name)) return false;
        switch (name.Trim().ToLowerInvariant())
        {
            case "cube":     shape = PartShape.Cube;     return true;
            case "sphere":   shape = PartShape.Sphere;   return true;
            case "ball":     shape = PartShape.Sphere;   return true;
            case "cylinder": shape = PartShape.Cylinder; return true;
            case "capsule":  shape = PartShape.Capsule;  return true;
            case "plane":    shape = PartShape.Plane;    return true;
            case "quad":     shape = PartShape.Quad;     return true;
        }
        return false;
    }

    private static PartShape SniffShape(GameObject go)
    {

        var mf = go.GetComponent<MeshFilter>();
        var meshName = mf != null && mf.sharedMesh != null ? mf.sharedMesh.name : null;
        if (string.IsNullOrEmpty(meshName)) return PartShape.Cube;
        switch (meshName.ToLowerInvariant())
        {
            case "sphere":   return PartShape.Sphere;
            case "cylinder": return PartShape.Cylinder;
            case "capsule":  return PartShape.Capsule;
            case "plane":    return PartShape.Plane;
            case "quad":     return PartShape.Quad;
            default:         return PartShape.Cube;
        }
    }

    private static void PropagateMoveToDescendants(LuaInstance instance, LuaCFrame oldCF, LuaCFrame newCF)
    {
        var delta = newCF * oldCF.Inverse();
        for (int i = 0; i < instance.Children.Count; i++)
            ApplyDeltaRecursive(instance.Children[i], delta);
    }

    private static void ApplyDeltaRecursive(LuaInstance node, LuaCFrame delta)
    {
        if (node.Moveable && node.ClassDef is BasePart && node.UserState is State cs)
        {
            cs.CFrame = delta * cs.CFrame;
            ApplyTransform(node, cs);
            node.FirePropertyChanged("CFrame");
        }
        for (int i = 0; i < node.Children.Count; i++)
            ApplyDeltaRecursive(node.Children[i], delta);
    }
}

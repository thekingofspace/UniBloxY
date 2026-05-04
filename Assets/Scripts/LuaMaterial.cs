using System.Collections.Generic;
using MoonSharp.Interpreter;
using UnityEngine;

[MoonSharpUserData]
public class LuaMaterial
{
    public string Name { get; }
    [MoonSharpHidden] public Material Source { get; }
    [MoonSharpHidden] private readonly List<Material> instances = new();

    public LuaMaterial(string name, Material source)
    {
        Name = name;
        Source = source;
    }

    [MoonSharpHidden]
    public Material CreateInstance()
    {
        var inst = new Material(Source);
        instances.Add(inst);
        return inst;
    }

    [MoonSharpHidden]
    public bool DropInstance(Material m) => instances.Remove(m);

    [MoonSharpHidden]
    public bool OwnsInstance(Material m) => instances.Contains(m);

    private void Apply(System.Action<Material> action)
    {
        if (Source != null) action(Source);
        for (int i = instances.Count - 1; i >= 0; i--)
        {
            var m = instances[i];
            if (m == null) instances.RemoveAt(i);
            else action(m);
        }
    }

    public LuaColor3 Color
    {
        get
        {
            if (Source == null || !Source.HasProperty("_Color")) return new LuaColor3(1f, 1f, 1f);
            var c = Source.GetColor("_Color");
            return new LuaColor3(c.r, c.g, c.b);
        }
        set
        {
            if (value == null) return;
            var c = new Color(value.R, value.G, value.B, 1f);
            Apply(m => { if (m.HasProperty("_Color")) m.SetColor("_Color", c); else m.color = c; });
        }
    }

    public LuaVector2 Tiling
    {
        get
        {
            if (Source == null) return LuaVector2.One;
            var t = Source.HasProperty("_MainTex") ? Source.GetTextureScale("_MainTex") : Vector2.one;
            return new LuaVector2(t.x, t.y);
        }
        set
        {
            if (value == null) return;
            var t = new Vector2(value.X, value.Y);
            Apply(m => { if (m.HasProperty("_MainTex")) m.SetTextureScale("_MainTex", t); });
        }
    }

    // Accepts either a number (uniform scale on both axes) or a Vector2
    // (per-axis). Stored / read back as a Vector2.
    public object TileSize
    {
        get
        {
            if (Source == null) return LuaVector2.One;
            var t = Source.HasProperty("_MainTex") ? Source.GetTextureScale("_MainTex") : Vector2.one;
            return new LuaVector2(t.x, t.y);
        }
        set
        {
            Vector2 t;
            switch (value)
            {
                case null:
                    return;
                case LuaVector2 v2:
                    t = new Vector2(v2.X, v2.Y);
                    break;
                case double d:
                    t = new Vector2((float)d, (float)d);
                    break;
                case float f:
                    t = new Vector2(f, f);
                    break;
                case int i:
                    t = new Vector2(i, i);
                    break;
                default:
                    throw new ScriptRuntimeException(
                        "Material.TileSize must be a number or a Vector2");
            }
            Apply(m => { if (m.HasProperty("_MainTex")) m.SetTextureScale("_MainTex", t); });
        }
    }

    public LuaVector2 Offset
    {
        get
        {
            if (Source == null) return LuaVector2.Zero;
            var t = Source.HasProperty("_MainTex") ? Source.GetTextureOffset("_MainTex") : Vector2.zero;
            return new LuaVector2(t.x, t.y);
        }
        set
        {
            if (value == null) return;
            var t = new Vector2(value.X, value.Y);
            Apply(m => { if (m.HasProperty("_MainTex")) m.SetTextureOffset("_MainTex", t); });
        }
    }

    public LuaTexture Texture
    {
        get
        {
            if (Source == null || !Source.HasProperty("_MainTex")) return null;
            var tex = Source.GetTexture("_MainTex");
            return tex == null ? null : new LuaTexture(tex.name, tex);
        }
        set
        {
            var tex = value?.Texture;
            Apply(m => { if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex); });
        }
    }

    public float Smoothness
    {
        get => (Source != null && Source.HasProperty("_Smoothness")) ? Source.GetFloat("_Smoothness")
             : (Source != null && Source.HasProperty("_Glossiness")) ? Source.GetFloat("_Glossiness") : 0f;
        set
        {
            Apply(m =>
            {
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", value);
                if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", value);
            });
        }
    }

    public float Metallic
    {
        get => (Source != null && Source.HasProperty("_Metallic")) ? Source.GetFloat("_Metallic") : 0f;
        set => Apply(m => { if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", value); });
    }

    public int RenderQueue
    {
        get => Source != null ? Source.renderQueue : -1;
        set => Apply(m => m.renderQueue = value);
    }

    public LuaShader Shader
    {
        get => Source == null ? null : new LuaShader(Source.shader != null ? Source.shader.name : "", Source.shader);
        set
        {
            if (value?.Shader == null) return;
            Apply(m => m.shader = value.Shader);
        }
    }

    public void Set(string prop, DynValue value)
    {
        if (string.IsNullOrEmpty(prop))
            throw new ScriptRuntimeException("Material:Set requires a property name");
        Apply(m => MaterialProps.Apply(m, prop, value));
    }

    public DynValue Get(string prop)
    {
        if (Source == null || string.IsNullOrEmpty(prop) || !Source.HasProperty(prop))
            return DynValue.Nil;
        return MaterialProps.Read(Source, prop);
    }

    public void EnableKeyword(string keyword) => Apply(m => m.EnableKeyword(keyword));
    public void DisableKeyword(string keyword) => Apply(m => m.DisableKeyword(keyword));

    public override string ToString() => $"Material<{Name}>";
}

public static class MaterialProps
{
    public static void Apply(Material mat, string prop, DynValue value)
    {
        if (mat == null) return;
        switch (value.Type)
        {
            case DataType.Number:
                mat.SetFloat(prop, (float)value.Number);
                break;
            case DataType.Boolean:
                mat.SetInt(prop, value.Boolean ? 1 : 0);
                break;
            case DataType.UserData:
                var obj = value.UserData.Object;
                if (obj is LuaVector3 v3)
                    mat.SetVector(prop, new Vector4(v3.X, v3.Y, v3.Z, 0f));
                else if (obj is LuaVector2 v2)
                    mat.SetVector(prop, new Vector4(v2.X, v2.Y, 0f, 0f));
                else if (obj is LuaColor3 c3)
                    mat.SetColor(prop, new Color(c3.R, c3.G, c3.B, 1f));
                else if (obj is Color col)
                    mat.SetColor(prop, col);
                else if (obj is Vector4 v4)
                    mat.SetVector(prop, v4);
                else if (obj is LuaTexture lt)
                    mat.SetTexture(prop, lt.Texture);
                else if (obj is Texture tex)
                    mat.SetTexture(prop, tex);
                else
                    throw new ScriptRuntimeException($"Material.Set: unsupported value type for \"{prop}\"");
                break;
            case DataType.Nil:
            case DataType.Void:
                break;
            default:
                throw new ScriptRuntimeException($"Material.Set: unsupported value type for \"{prop}\"");
        }
    }

    public static DynValue Read(Material mat, string prop)
    {
        if (mat == null || !mat.HasProperty(prop)) return DynValue.Nil;

        var shader = mat.shader;
        if (shader != null)
        {
            int idx = shader.FindPropertyIndex(prop);
            if (idx >= 0)
            {
                var t = shader.GetPropertyType(idx);
                switch (t)
                {
                    case UnityEngine.Rendering.ShaderPropertyType.Color:
                        var c = mat.GetColor(prop);
                        return UserData.Create(new LuaColor3(c.r, c.g, c.b));
                    case UnityEngine.Rendering.ShaderPropertyType.Vector:
                        var v = mat.GetVector(prop);
                        return UserData.Create(new LuaVector3(v.x, v.y, v.z));
                    case UnityEngine.Rendering.ShaderPropertyType.Float:
                    case UnityEngine.Rendering.ShaderPropertyType.Range:
                        return DynValue.NewNumber(mat.GetFloat(prop));
                    case UnityEngine.Rendering.ShaderPropertyType.Texture:
                        var tex = mat.GetTexture(prop);
                        return tex == null ? DynValue.Nil : UserData.Create(new LuaTexture(tex.name, tex));
                }
            }
        }
        return DynValue.Nil;
    }
}

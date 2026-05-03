using MoonSharp.Interpreter;
using UnityEngine;

[MoonSharpUserData]
public class LuaTexture
{
    public string Name { get; }
    [MoonSharpHidden] public Texture Texture { get; }

    public int Width => Texture != null ? Texture.width : 0;
    public int Height => Texture != null ? Texture.height : 0;

    public string WrapMode
    {
        get => Texture != null ? Texture.wrapMode.ToString() : "Repeat";
        set
        {
            if (Texture == null) return;
            if (System.Enum.TryParse<TextureWrapMode>(value, true, out var m))
                Texture.wrapMode = m;
        }
    }

    public string FilterMode
    {
        get => Texture != null ? Texture.filterMode.ToString() : "Bilinear";
        set
        {
            if (Texture == null) return;
            if (System.Enum.TryParse<FilterMode>(value, true, out var m))
                Texture.filterMode = m;
        }
    }

    public LuaTexture(string name, Texture texture)
    {
        Name = name;
        Texture = texture;
    }

    public override string ToString() => $"Texture<{Name}>";
}

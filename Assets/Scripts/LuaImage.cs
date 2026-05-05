using MoonSharp.Interpreter;
using UnityEngine;

[MoonSharpUserData]
public class LuaImage
{
    public string ClassName => "Image";
    public string Name { get; }
    [MoonSharpHidden] public Sprite Sprite { get; private set; }
    [MoonSharpHidden] public Texture2D Texture { get; }

    public int Width => Sprite != null ? (int)Sprite.rect.width
                       : Texture != null ? Texture.width : 0;
    public int Height => Sprite != null ? (int)Sprite.rect.height
                        : Texture != null ? Texture.height : 0;

    public LuaImage(string name, Sprite sprite)
    {
        Name = name;
        Sprite = sprite;
        Texture = sprite != null ? sprite.texture : null;
    }

    public LuaImage(string name, Texture2D texture)
    {
        Name = name;
        Texture = texture;
        if (texture != null)
        {
            Sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            Sprite.name = name;
        }
    }

    public override string ToString() => $"Image<{Name}>";
}

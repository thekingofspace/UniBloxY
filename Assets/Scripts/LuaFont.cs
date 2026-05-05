using MoonSharp.Interpreter;
using UnityEngine;

[MoonSharpUserData]
public class LuaFont
{
    public string ClassName => "Font";
    public string Name { get; }
    [MoonSharpHidden] public Font Font { get; }

    public LuaFont(string name, Font font)
    {
        Name = name;
        Font = font;
    }

    public override string ToString() => $"Font<{Name}>";
}

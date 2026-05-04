using MoonSharp.Interpreter;
using UnityEngine;

[MoonSharpUserData]
public class LuaShader
{
    public string ClassName => "Shader";
    public string Name { get; }
    public Shader Shader { get; }

    public LuaShader(string name, Shader shader)
    {
        Name = name;
        Shader = shader;
    }

    public override string ToString() => $"Shader<{Name}>";
}

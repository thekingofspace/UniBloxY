using MoonSharp.Interpreter;
using UnityEngine;

[MoonSharpUserData]
public class LuaMesh
{
    public string ClassName => "Mesh";
    public string Name { get; }
    [MoonSharpHidden] public Mesh Mesh { get; }

    public int VertexCount => Mesh != null ? Mesh.vertexCount : 0;
    public int SubMeshCount => Mesh != null ? Mesh.subMeshCount : 0;

    public LuaMesh(string name, Mesh mesh)
    {
        Name = name;
        Mesh = mesh;
    }

    public override string ToString() => $"Mesh<{Name}>";
}

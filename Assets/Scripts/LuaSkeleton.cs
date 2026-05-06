using MoonSharp.Interpreter;
using UnityEngine;

[MoonSharpUserData]
public class LuaSkeleton
{
    public string ClassName => "Skeleton";
    public string Name { get; }
    [MoonSharpHidden] public GameObject Prefab { get; }

    public LuaSkeleton(string name, GameObject prefab)
    {
        Name = name;
        Prefab = prefab;
    }

    public override string ToString() => $"Skeleton<{Name}>";
}

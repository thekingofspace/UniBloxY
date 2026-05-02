using UnityEngine;

public class Folder : LuaInstanceClass
{
    public override string ClassName => "Folder";

    public override void Initialize(LuaInstance instance)
    {
        instance.UnityObject = new GameObject(instance.Name);
    }
}

using UnityEngine;

public class Folder : LuaInstanceClass
{
    public override string ClassName => "Folder";

    public override void OnEnterScene(LuaInstance instance)
    {
        if (instance.UnityObject != null) return;
        var go = new GameObject(instance.Name);
        instance.UnityObject = go;
        var parentGo = instance.Parent?.UnityObject;
        if (parentGo != null)
            go.transform.SetParent(parentGo.transform, true);
    }

    public override void OnExitScene(LuaInstance instance)
    {
        if (instance.UnityObject != null)
        {
            Object.Destroy(instance.UnityObject);
            instance.UnityObject = null;
        }
    }
}

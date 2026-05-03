using UnityEngine;

public class RenderGroup : Renderable
{
    public override string ClassName => "RenderGroup";

    public override bool ParentsUnityObject => false;

    public override void OnEnterScene(LuaInstance instance)
    {
        if (instance.UnityObject != null) return;
        var go = new GameObject(instance.Name);
        instance.UnityObject = go;
    }

    public override void OnExitScene(LuaInstance instance)
    {
        if (instance.UnityObject != null)
        {
            Object.Destroy(instance.UnityObject);
            instance.UnityObject = null;
        }
    }

    public override void OnChildAdded(LuaInstance instance, LuaInstance child)
    {
        if (!GetRender(instance)) return;
        EnableNonGroupSubtree(child);
    }

    protected override void OnRenderFlagChanged(LuaInstance instance, bool newValue)
    {
        if (!newValue) return;
        for (int i = 0; i < instance.Children.Count; i++)
            EnableNonGroupSubtree(instance.Children[i]);
    }

    private static void EnableNonGroupSubtree(LuaInstance node)
    {
        if (node.ClassDef is RenderGroup) return;
        if (node.ClassDef is Renderable r)
            r.SetRender(node, true);
        for (int i = 0; i < node.Children.Count; i++)
            EnableNonGroupSubtree(node.Children[i]);
    }
}

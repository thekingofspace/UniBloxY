using System.Runtime.CompilerServices;
using MoonSharp.Interpreter;
using UnityEngine;

public class RenderGroup : Renderable
{
    public override string ClassName => "RenderGroup";

    public override bool ParentsUnityObject => false;

    private class GroupData
    {
        public bool OverrideParent = false;
    }

    private static readonly ConditionalWeakTable<LuaInstance, GroupData> data = new();

    private static GroupData Get(LuaInstance instance) =>
        data.GetValue(instance, _ => new GroupData());

    protected override bool BlocksParentRenderChain(LuaInstance instance) =>
        Get(instance).OverrideParent;

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

    public override bool TryGetProperty(LuaInstance instance, string key, out DynValue value)
    {
        if (key == "OverrideParent")
        {
            value = DynValue.NewBoolean(Get(instance).OverrideParent);
            return true;
        }
        return base.TryGetProperty(instance, key, out value);
    }

    public override bool TrySetProperty(LuaInstance instance, string key, DynValue value)
    {
        if (key == "OverrideParent")
        {
            if (value.Type != DataType.Boolean)
                throw new ScriptRuntimeException("OverrideParent must be a boolean");
            var d = Get(instance);
            if (d.OverrideParent == value.Boolean) return true;
            d.OverrideParent = value.Boolean;
            RefreshRenderSubtree(instance);
            return true;
        }
        return base.TrySetProperty(instance, key, value);
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

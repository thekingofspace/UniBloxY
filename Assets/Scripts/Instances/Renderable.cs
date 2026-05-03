using System.Runtime.CompilerServices;
using MoonSharp.Interpreter;

public abstract class Renderable : LuaInstanceClass
{
    private class RenderableData
    {
        public bool Render = false;
    }

    private static readonly ConditionalWeakTable<LuaInstance, RenderableData> data = new();

    private static RenderableData Get(LuaInstance instance) =>
        data.GetValue(instance, _ => new RenderableData());

    public bool GetRender(LuaInstance instance) => Get(instance).Render;

    public void SetRender(LuaInstance instance, bool value) => Get(instance).Render = value;

    public static bool EffectiveRender(LuaInstance instance)
    {
        var node = instance;
        while (node != null)
        {
            if (node.ClassDef is Renderable r && !r.GetRender(node))
                return false;
            node = node.Parent;
        }
        return true;
    }

    protected virtual void OnRenderStateChanged(LuaInstance instance) { }

    protected virtual void OnRenderFlagChanged(LuaInstance instance, bool newValue) { }

    protected static void RefreshRenderSubtree(LuaInstance node) => RefreshSubtree(node);

    public override void OnAncestryChanged(LuaInstance instance)
    {
        OnRenderStateChanged(instance);
    }

    private static void RefreshSubtree(LuaInstance node)
    {
        if (node.ClassDef is Renderable r)
            r.OnRenderStateChanged(node);
        for (int i = 0; i < node.Children.Count; i++)
            RefreshSubtree(node.Children[i]);
    }

    public override bool TryGetProperty(LuaInstance instance, string key, out DynValue value)
    {
        if (key == "Render")
        {
            value = DynValue.NewBoolean(Get(instance).Render);
            return true;
        }
        value = DynValue.Nil;
        return false;
    }

    public override bool TrySetProperty(LuaInstance instance, string key, DynValue value)
    {
        if (key == "Render")
        {
            if (value.Type != DataType.Boolean)
                throw new ScriptRuntimeException("Render must be a boolean");
            var d = Get(instance);
            if (d.Render == value.Boolean) return true;
            d.Render = value.Boolean;
            OnRenderFlagChanged(instance, d.Render);
            RefreshSubtree(instance);
            return true;
        }
        return false;
    }
}

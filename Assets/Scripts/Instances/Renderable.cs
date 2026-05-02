using System.Runtime.CompilerServices;
using MoonSharp.Interpreter;

public abstract class Renderable : LuaInstanceClass
{
    private class RenderableData
    {
        public bool Render = true;
    }

    private static readonly ConditionalWeakTable<LuaInstance, RenderableData> data = new();

    private static RenderableData Get(LuaInstance instance) =>
        data.GetValue(instance, _ => new RenderableData());

    public bool GetRender(LuaInstance instance) => Get(instance).Render;

    public void SetRender(LuaInstance instance, bool value) => Get(instance).Render = value;

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
            Get(instance).Render = value.Boolean;
            return true;
        }
        return false;
    }
}

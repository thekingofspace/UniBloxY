using MoonSharp.Interpreter;
using UnityEngine;

public class AreaLight : LightElement
{
    public override string ClassName => "AreaLight";

    private class AreaState : LightState
    {
        public LuaVector2 Size = new LuaVector2(1f, 1f);
    }

    protected override LightState NewState() => new AreaState { Size = new LuaVector2(1f, 1f) };

    protected override void ConfigureType(Light light)
    {
        light.type = LightType.Rectangle;
        // Unity's rect/area lights only contribute via baking — RealTime=false
        // is the meaningful default here, but we leave it driven by the user.
    }

    protected override void Apply(LightState s)
    {
        base.Apply(s);
        if (s.Unity != null && s is AreaState a)
            s.Unity.areaSize = new Vector2(a.Size.X, a.Size.Y);
    }

    public override void ImportFromUnityObject(LuaInstance instance, GameObject go)
    {
        base.ImportFromUnityObject(instance, go);
        var light = go.GetComponent<Light>();
        if (light == null) return;
        ((AreaState)instance.UserState).Size = new LuaVector2(light.areaSize.x, light.areaSize.y);
    }

    public override bool TryGetProperty(LuaInstance instance, string key, out DynValue value)
    {
        if (key == "Size")
        {
            value = UserData.Create(((AreaState)instance.UserState).Size);
            return true;
        }
        return base.TryGetProperty(instance, key, out value);
    }

    public override bool TrySetProperty(LuaInstance instance, string key, DynValue value)
    {
        if (key == "Size")
        {
            if (value.Type != DataType.UserData || value.UserData.Object is not LuaVector2 v2)
                throw new ScriptRuntimeException("AreaLight.Size must be a Vector2");
            var s = (AreaState)instance.UserState;
            s.Size = v2;
            if (s.Unity != null) s.Unity.areaSize = new Vector2(v2.X, v2.Y);
            return true;
        }
        return base.TrySetProperty(instance, key, value);
    }
}

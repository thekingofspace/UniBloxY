using MoonSharp.Interpreter;
using UnityEngine;

public class Spotlight : LightElement
{
    public override string ClassName => "Spotlight";

    private class SpotState : LightState
    {
        public float Angle = 45f;
    }

    protected override LightState NewState() => new SpotState { Angle = 45f };

    protected override void ConfigureType(Light light)
    {
        light.type = LightType.Spot;
    }

    protected override void Apply(LightState s)
    {
        base.Apply(s);
        if (s.Unity != null && s is SpotState ss) s.Unity.spotAngle = ss.Angle;
    }

    public override void ImportFromUnityObject(LuaInstance instance, GameObject go)
    {
        base.ImportFromUnityObject(instance, go);
        var light = go.GetComponent<Light>();
        if (light == null) return;
        ((SpotState)instance.UserState).Angle = light.spotAngle;
    }

    public override bool TryGetProperty(LuaInstance instance, string key, out DynValue value)
    {
        if (key == "Angle")
        {
            value = DynValue.NewNumber(((SpotState)instance.UserState).Angle);
            return true;
        }
        return base.TryGetProperty(instance, key, out value);
    }

    public override bool TrySetProperty(LuaInstance instance, string key, DynValue value)
    {
        if (key == "Angle")
        {
            if (value.Type != DataType.Number) throw new ScriptRuntimeException("Spotlight.Angle must be a number");
            var s = (SpotState)instance.UserState;
            s.Angle = (float)value.Number;
            if (s.Unity != null) s.Unity.spotAngle = s.Angle;
            return true;
        }
        return base.TrySetProperty(instance, key, value);
    }
}

using MoonSharp.Interpreter;
using UnityEngine;

public abstract class LightElement : LuaInstanceClass
{
    // Lights follow their parent's Unity transform — this lets the parent's
    // CFrame drive the light's world position/rotation.
    public override bool ParentsUnityObject => true;

    protected class LightState
    {
        public LuaColor3 Color = new LuaColor3(1f, 1f, 1f);
        public float Intensity = 1f;
        public float Range = 10f;
        public float Brightness = 1f;
        public string ShadowType = "Soft";   // "Soft" | "Realistic"
        public bool Active = true;
        public bool RealTime = true;
        public float NearPlane = 0.2f;
        public float Strength = 1f;

        public Light Unity;
        public GameObject Owner;
    }

    public override void Initialize(LuaInstance instance)
    {
        instance.UserState = NewState();
    }

    protected virtual LightState NewState() => new LightState();

    public override void OnEnterScene(LuaInstance instance)
    {
        var s = (LightState)instance.UserState;
        if (s.Owner != null) return;

        // If an UnityObject was already attached (via ObjectAsInstance /
        // ConvertToInstance), bind to the existing GameObject + Light
        // component instead of creating a fresh one.
        GameObject go;
        if (instance.UnityObject != null)
        {
            go = instance.UnityObject;
        }
        else
        {
            go = new GameObject(instance.Name);
            instance.UnityObject = go;
        }
        s.Owner = go;

        var light = go.GetComponent<Light>();
        if (light == null) light = go.AddComponent<Light>();
        s.Unity = light;

        ConfigureType(light);
        Apply(s);
    }

    public override void ImportFromUnityObject(LuaInstance instance, GameObject go)
    {
        // Read existing properties off the GameObject's Light component so
        // wrapping a scene light reflects its current configuration.
        var s = (LightState)instance.UserState;
        var light = go.GetComponent<Light>();
        if (light == null) return;
        s.Color = new LuaColor3(light.color.r, light.color.g, light.color.b);
        s.Intensity = light.intensity;
        s.Range = light.range;
        s.Brightness = 1f;
        s.Active = light.enabled;
        s.RealTime = light.lightmapBakeType == UnityEngine.LightmapBakeType.Realtime;
        s.NearPlane = light.shadowNearPlane;
        s.Strength = light.shadowStrength;
        s.ShadowType = light.shadows == UnityEngine.LightShadows.Soft ? "Soft" : "Realistic";
    }

    public override void OnAncestryChanged(LuaInstance instance)
    {
        // Lights take their world placement from the parent. After a
        // reparent, snap the local transform to identity so the light's
        // world rotation matches the new parent's (instead of preserving
        // its old world rotation, which is Unity's default for SetParent).
        if (instance.UnityObject != null)
        {
            instance.UnityObject.transform.localPosition = UnityEngine.Vector3.zero;
            instance.UnityObject.transform.localRotation = UnityEngine.Quaternion.identity;
        }
    }

    public override void OnExitScene(LuaInstance instance)
    {
        var s = (LightState)instance.UserState;
        if (s.Owner != null)
        {
            Object.Destroy(s.Owner);
            s.Owner = null;
            s.Unity = null;
        }
        instance.UnityObject = null;
    }

    protected abstract void ConfigureType(Light light);

    public override bool TryGetProperty(LuaInstance instance, string key, out DynValue value)
    {
        var s = (LightState)instance.UserState;
        switch (key)
        {
            case "Color":      value = UserData.Create(s.Color); return true;
            case "Intensity":  value = DynValue.NewNumber(s.Intensity); return true;
            case "Range":      value = DynValue.NewNumber(s.Range); return true;
            case "Brightness": value = DynValue.NewNumber(s.Brightness); return true;
            case "ShadowType": value = DynValue.NewString(s.ShadowType); return true;
            case "Active":     value = DynValue.NewBoolean(s.Active); return true;
            case "RealTime":   value = DynValue.NewBoolean(s.RealTime); return true;
            case "NearPlane":  value = DynValue.NewNumber(s.NearPlane); return true;
            case "Strength":   value = DynValue.NewNumber(s.Strength); return true;
        }
        value = DynValue.Nil;
        return false;
    }

    public override bool TrySetProperty(LuaInstance instance, string key, DynValue value)
    {
        var s = (LightState)instance.UserState;
        switch (key)
        {
            case "Color":
                if (value.Type == DataType.UserData && value.UserData.Object is LuaColor3 c)
                { s.Color = c; Apply(s); return true; }
                throw new ScriptRuntimeException("Light.Color must be a Color3");
            case "Intensity":
                if (value.Type != DataType.Number) throw new ScriptRuntimeException("Light.Intensity must be a number");
                s.Intensity = (float)value.Number; Apply(s); return true;
            case "Range":
                if (value.Type != DataType.Number) throw new ScriptRuntimeException("Light.Range must be a number");
                s.Range = (float)value.Number; Apply(s); return true;
            case "Brightness":
                if (value.Type != DataType.Number) throw new ScriptRuntimeException("Light.Brightness must be a number");
                s.Brightness = (float)value.Number; Apply(s); return true;
            case "ShadowType":
                if (value.Type != DataType.String) throw new ScriptRuntimeException("Light.ShadowType must be a string");
                s.ShadowType = value.String; Apply(s); return true;
            case "Active":
                if (value.Type != DataType.Boolean) throw new ScriptRuntimeException("Light.Active must be a boolean");
                s.Active = value.Boolean; Apply(s); return true;
            case "RealTime":
                if (value.Type != DataType.Boolean) throw new ScriptRuntimeException("Light.RealTime must be a boolean");
                s.RealTime = value.Boolean; Apply(s); return true;
            case "NearPlane":
                if (value.Type != DataType.Number) throw new ScriptRuntimeException("Light.NearPlane must be a number");
                s.NearPlane = (float)value.Number; Apply(s); return true;
            case "Strength":
                if (value.Type != DataType.Number) throw new ScriptRuntimeException("Light.Strength must be a number");
                s.Strength = (float)value.Number; Apply(s); return true;
        }
        return false;
    }

    protected virtual void Apply(LightState s)
    {
        var light = s.Unity;
        if (light == null) return;
        light.color = new Color(s.Color.R, s.Color.G, s.Color.B, 1f);
        light.intensity = s.Intensity * s.Brightness;
        light.range = s.Range;
        light.enabled = s.Active;
        light.shadows = s.ShadowType == "Realistic" ? LightShadows.Hard : LightShadows.Soft;
        light.lightmapBakeType = s.RealTime ? LightmapBakeType.Realtime : LightmapBakeType.Baked;
        if (s.RealTime)
        {
            light.shadowNearPlane = s.NearPlane;
            light.shadowStrength = Mathf.Clamp01(s.Strength);
        }
    }
}

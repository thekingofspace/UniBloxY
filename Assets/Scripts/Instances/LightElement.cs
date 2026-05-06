using MoonSharp.Interpreter;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public abstract class LightElement : LuaInstanceClass
{

    public override bool ParentsUnityObject => true;

    protected class LightState
    {
        public LuaColor3 Color = new LuaColor3(1f, 1f, 1f);
        public float Intensity = 1f;
        public float Range = 10f;
        public float Brightness = 1f;
        public string ShadowType = "Soft";
        public bool Active = true;
        public bool RealTime = true;
        public float NearPlane = 0.2f;
        public float Strength = 1f;
        // Pose override — applied after the per-frame ancestry reset so a
        // directional light can be aimed (e.g. sun angled down from above).
        public LuaVector3 Rotation = LuaVector3.Zero;

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

        var s = (LightState)instance.UserState;
        var light = go.GetComponent<Light>();
        if (light == null) return;
        s.Color = new LuaColor3(light.color.r, light.color.g, light.color.b);
        s.Intensity = light.intensity;
        s.Range = light.range;
        s.Brightness = 1f;
        s.Active = light.enabled;
#if UNITY_EDITOR
        s.RealTime = light.lightmapBakeType == UnityEngine.LightmapBakeType.Realtime;
#endif
        s.NearPlane = light.shadowNearPlane;
        s.Strength = light.shadowStrength;
        s.ShadowType = light.shadows == UnityEngine.LightShadows.Soft ? "Soft" : "Realistic";
    }

    public override void OnAncestryChanged(LuaInstance instance)
    {

        if (instance.UnityObject != null)
        {
            var s = (LightState)instance.UserState;
            instance.UnityObject.transform.localPosition = UnityEngine.Vector3.zero;
            instance.UnityObject.transform.localRotation = UnityEngine.Quaternion.Euler(s.Rotation.X, s.Rotation.Y, s.Rotation.Z);
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
            case "Rotation":   value = UserData.Create(s.Rotation); return true;
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
            case "Rotation":
                if (value.Type == DataType.UserData && value.UserData.Object is LuaVector3 r)
                {
                    s.Rotation = r;
                    if (instance.UnityObject != null)
                        instance.UnityObject.transform.localRotation = UnityEngine.Quaternion.Euler(r.X, r.Y, r.Z);
                    return true;
                }
                throw new ScriptRuntimeException("Light.Rotation must be a Vector3 (Euler degrees)");
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
#if UNITY_EDITOR
        light.lightmapBakeType = s.RealTime ? LightmapBakeType.Realtime : LightmapBakeType.Baked;
#endif
        if (s.RealTime)
        {
            light.shadowNearPlane = s.NearPlane;
            light.shadowStrength = Mathf.Clamp01(s.Strength);
        }
    }
}

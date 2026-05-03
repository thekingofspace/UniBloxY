using MoonSharp.Interpreter;
using UnityEngine;

public class Camera : LuaInstanceClass
{
    public override string ClassName => "Camera";

    private class State
    {
        public LuaCFrame CFrame = new LuaCFrame(LuaVector3.Zero, LuaVector3.Zero);
        public float FOV = 60f;
        public UnityEngine.Camera Cam;
    }

    public override void Initialize(LuaInstance instance)
    {
        instance.Indestructible = true;
        instance.Reparentable = false;
        instance.UserState = new State();

        instance.Table["GetScreenSize"] = DynValue.NewCallback((ctx, args) =>
        {
            var s = (State)instance.UserState;
            if (s.Cam != null)
            {
                var r = s.Cam.pixelRect;
                return UserData.Create(new LuaVector2(r.width, r.height));
            }
            return UserData.Create(new LuaVector2(Screen.width, Screen.height));
        });

        instance.Table["GetWindowSize"] = DynValue.NewCallback((ctx, args) =>
        {
            return UserData.Create(new LuaVector2(Screen.width, Screen.height));
        });

        instance.Table["GetViewSize"] = DynValue.NewCallback((ctx, args) =>
        {
            var s = (State)instance.UserState;
            float distance = 10f;
            if (args.Count >= 1 && args[0].Type == DataType.Number)
                distance = (float)args[0].Number;
            else if (args.Count >= 2 && args[1].Type == DataType.Number)
                distance = (float)args[1].Number;

            float aspect = GetAspect(s);
            float fov = s.FOV;
            float height = 2f * Mathf.Abs(distance) * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            float width = height * aspect;
            return UserData.Create(new LuaVector2(width, height));
        });
    }

    private static float GetAspect(State s)
    {
        if (s.Cam != null && s.Cam.pixelHeight > 0)
            return s.Cam.pixelWidth / (float)s.Cam.pixelHeight;
        if (Screen.height > 0)
            return Screen.width / (float)Screen.height;
        return 1f;
    }

    public override bool TryGetProperty(LuaInstance instance, string key, out DynValue value)
    {
        var s = (State)instance.UserState;
        switch (key)
        {
            case "CFrame": value = UserData.Create(s.CFrame); return true;
            case "FOV": value = DynValue.NewNumber(s.FOV); return true;
            case "Aspect": value = DynValue.NewNumber(GetAspect(s)); return true;
        }
        value = DynValue.Nil;
        return false;
    }

    public override bool TrySetProperty(LuaInstance instance, string key, DynValue value)
    {
        var s = (State)instance.UserState;
        switch (key)
        {
            case "CFrame":
                if (value.Type == DataType.UserData && value.UserData.Object is LuaCFrame cf)
                {
                    s.CFrame = cf;
                    ApplyTransform(s);
                    return true;
                }
                throw new ScriptRuntimeException("Camera.CFrame must be a CFrame");
            case "FOV":
                if (value.Type == DataType.Number)
                {
                    s.FOV = (float)value.Number;
                    if (s.Cam != null) s.Cam.fieldOfView = s.FOV;
                    return true;
                }
                throw new ScriptRuntimeException("Camera.FOV must be a number");
        }
        return false;
    }

    public override void OnEnterScene(LuaInstance instance)
    {
        if (instance.UnityObject != null) return;
        var s = (State)instance.UserState;

        var existing = UnityEngine.Camera.main
            ?? Object.FindAnyObjectByType<UnityEngine.Camera>(FindObjectsInactive.Include);
        GameObject go;
        UnityEngine.Camera cam;
        if (existing != null)
        {
            cam = existing;
            go = existing.gameObject;
            var t = go.transform;
            s.CFrame = new LuaCFrame(
                new LuaVector3(t.position.x, t.position.y, t.position.z),
                new LuaVector3(t.eulerAngles.x, t.eulerAngles.y, t.eulerAngles.z)
            );
            s.FOV = cam.fieldOfView;
        }
        else
        {
            go = new GameObject(instance.Name);
            cam = go.AddComponent<UnityEngine.Camera>();
            if (go.GetComponent<AudioListener>() == null) go.AddComponent<AudioListener>();
            go.tag = "MainCamera";
            cam.fieldOfView = s.FOV;
        }

        s.Cam = cam;
        instance.UnityObject = go;
        ApplyTransform(s);
    }

    private static void ApplyTransform(State s)
    {
        if (s.Cam == null) return;
        var p = s.CFrame.Position;
        var r = s.CFrame.Rotation;
        s.Cam.transform.position = new Vector3(p.X, p.Y, p.Z);
        s.Cam.transform.eulerAngles = new Vector3(r.X, r.Y, r.Z);
    }
}

using MoonSharp.Interpreter;
using UnityEngine;

public class Camera : LuaInstanceClass
{
    public override string ClassName => "Camera";

    internal class State
    {
        public LuaCFrame CFrame = new LuaCFrame(LuaVector3.Zero, LuaVector3.Zero);
        public float FOV = 60f;
        public UnityEngine.Camera Cam;
        public Signal WindowResized;
        public int LastWidth;
        public int LastHeight;
        public CameraResizeWatcher Watcher;
    }

    public override void Initialize(LuaInstance instance)
    {
        instance.Indestructible = true;
        instance.Reparentable = false;

        var s = new State();
        s.WindowResized = new Signal(instance.Script, "Camera.WindowResized");
        s.LastWidth = Screen.width;
        s.LastHeight = Screen.height;
        instance.UserState = s;

        instance.Table["WindowResized"] = s.WindowResized.BuildTable();

        instance.Table["GetScreenSize"] = DynValue.NewCallback((ctx, args) =>
        {
            var st = (State)instance.UserState;
            if (st.Cam != null)
            {
                var r = st.Cam.pixelRect;
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
            var st = (State)instance.UserState;
            float distance = 10f;
            if (args.Count >= 1 && args[0].Type == DataType.Number)
                distance = (float)args[0].Number;
            else if (args.Count >= 2 && args[1].Type == DataType.Number)
                distance = (float)args[1].Number;

            float aspect = GetAspect(st);
            float fov = st.FOV;
            float height = 2f * Mathf.Abs(distance) * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            float width = height * aspect;
            return UserData.Create(new LuaVector2(width, height));
        });

        instance.Table["SetFullScreen"] = DynValue.NewCallback((ctx, args) =>
        {
            // (mode [, width, height])
            //   "borderless" → FullScreenWindow (no border, desktop res by default)
            //   "fullscreen" / "exclusive" → ExclusiveFullScreen
            //   "windowed" / "border" → Windowed; width/height optional
            string mode = args.Count > 1 && args[1].Type == DataType.String
                ? args[1].String.ToLowerInvariant() : "borderless";

            int w = Screen.width, h = Screen.height;
            if (args.Count > 2 && args[2].Type == DataType.Number) w = (int)args[2].Number;
            if (args.Count > 3 && args[3].Type == DataType.Number) h = (int)args[3].Number;

            FullScreenMode fsm;
            switch (mode)
            {
                case "fullscreen":
                case "exclusive":
                    fsm = FullScreenMode.ExclusiveFullScreen;
                    if (args.Count <= 2) { w = Display.main.systemWidth; h = Display.main.systemHeight; }
                    break;
                case "windowed":
                case "border":
                case "bordered":
                    fsm = FullScreenMode.Windowed;
                    break;
                case "maximized":
                    fsm = FullScreenMode.MaximizedWindow;
                    if (args.Count <= 2) { w = Display.main.systemWidth; h = Display.main.systemHeight; }
                    break;
                case "borderless":
                default:
                    fsm = FullScreenMode.FullScreenWindow;
                    if (args.Count <= 2) { w = Display.main.systemWidth; h = Display.main.systemHeight; }
                    break;
            }

            Screen.SetResolution(w, h, fsm);
            return DynValue.Nil;
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
            case "FullScreenMode": value = DynValue.NewString(Screen.fullScreenMode.ToString()); return true;
            case "IsFullScreen": value = DynValue.NewBoolean(Screen.fullScreen); return true;
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

        // Polls Screen size each Update and fires WindowResized when it changes.
        s.Watcher = go.GetComponent<CameraResizeWatcher>() ?? go.AddComponent<CameraResizeWatcher>();
        s.Watcher.Bind(s);
    }

    private static void ApplyTransform(State s)
    {
        if (s.Cam == null) return;
        var p = s.CFrame.Position;
        var r = s.CFrame.Rotation;
        s.Cam.transform.position = new Vector3(p.X, p.Y, p.Z);
        s.Cam.transform.eulerAngles = new Vector3(r.X, r.Y, r.Z);
    }

    internal class CameraResizeWatcher : MonoBehaviour
    {
        private State state;

        internal void Bind(State s)
        {
            state = s;
            state.LastWidth = Screen.width;
            state.LastHeight = Screen.height;
        }

        void Update()
        {
            if (state == null) return;
            int w = Screen.width, h = Screen.height;
            if (w != state.LastWidth || h != state.LastHeight)
            {
                state.LastWidth = w;
                state.LastHeight = h;
                state.WindowResized?.Fire(new LuaVector2(w, h));
            }
        }
    }
}

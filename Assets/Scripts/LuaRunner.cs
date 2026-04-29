using System.Collections.Generic;
using MoonSharp.Interpreter;
using UnityEngine;

public class LuaRunner : MonoBehaviour
{
    public static LuaRunner Instance { get; private set; }
    public Script Lua { get; private set; }

    private class HeartbeatBinding
    {
        public int Id;
        public double Importance;
        public string Name;
        public Closure Callback;
    }

    private readonly List<HeartbeatBinding> heartbeatBindings = new List<HeartbeatBinding>();
    private bool heartbeatDirty;
    private int nextHeartbeatId;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Lua = new Script();
        Lua.Options.ScriptLoader = new ResourceScriptLoader();

        UserData.RegisterType<LuaCFrame>();
        UserData.RegisterType<LuaCamera>();

        Lua.Globals["CFrame"] = LuaCFrame.BuildGlobal();

        Lua.Globals["print"] = new CallbackFunction((ctx, args) =>
        {
            var parts = new string[args.Count];
            for (int i = 0; i < args.Count; i++)
                parts[i] = args[i].ToPrintString();
            Debug.Log(string.Join("\t", parts));
            return DynValue.Nil;
        });

        Lua.Globals["Unity"] = CreateUnityTable();

        gameObject.AddComponent<InputGetter>().Initialize();

        var main = Resources.Load<TextAsset>("LuaScripts/main");

        if (main == null)
        {
            Debug.LogError("Could not find main.lua! Make sure it's at Assets/Resources/LuaScripts/main.lua");
            return;
        }

        try
        {
            Lua.DoString(main.text, null, "main.lua");
        }
        catch (ScriptRuntimeException ex)
        {
            Debug.LogError($"Lua error: {ex.DecoratedMessage}");
        }
    }

    void Update()
    {
        if (heartbeatBindings.Count == 0) return;

        if (heartbeatDirty)
        {
            heartbeatBindings.Sort((a, b) => b.Importance.CompareTo(a.Importance));
            heartbeatDirty = false;
        }

        var snapshot = heartbeatBindings.ToArray();
        float dt = Time.deltaTime;
        for (int i = 0; i < snapshot.Length; i++)
        {
            var binding = snapshot[i];
            try
            {
                Lua.Call(binding.Callback, dt);
            }
            catch (ScriptRuntimeException ex)
            {
                Debug.LogError($"Heartbeat '{binding.Name}' error: {ex.DecoratedMessage}");
            }
        }
    }

    private DynValue BindToHeartbeat(double importance, string name, Closure callback)
    {
        if (callback == null)
        {
            Debug.LogError($"BindToHeartbeat('{name}'): callback is nil");
            return DynValue.Nil;
        }

        var binding = new HeartbeatBinding
        {
            Id = ++nextHeartbeatId,
            Importance = importance,
            Name = name,
            Callback = callback,
        };
        heartbeatBindings.Add(binding);
        heartbeatDirty = true;

        System.Action unbind = () =>
        {
            for (int i = 0; i < heartbeatBindings.Count; i++)
            {
                if (heartbeatBindings[i].Id == binding.Id)
                {
                    heartbeatBindings.RemoveAt(i);
                    return;
                }
            }
        };
        return DynValue.FromObject(Lua, unbind);
    }

    private void Unbind(string name)
    {
        for (int i = heartbeatBindings.Count - 1; i >= 0; i--)
        {
            if (heartbeatBindings[i].Name == name)
                heartbeatBindings.RemoveAt(i);
        }
    }

    private Table CreateUnityTable()
    {
        var table = new Table(Lua);
        table["GetClass"] = (System.Func<string, Table>)((className) =>
        {
            var classTable = new Table(Lua);
            classTable["TestField"] = "hello from C#";
            return classTable;
        });
        table["BindToHeartbeat"] = (System.Func<double, string, Closure, DynValue>)BindToHeartbeat;
        table["Unbind"] = (System.Action<string>)Unbind;
        table["GetCamera"] = (System.Func<LuaCamera>)GetCamera;
        return table;
    }

    private LuaCamera GetCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("Unity.GetCamera(): no Camera tagged 'MainCamera' in the scene");
            return null;
        }
        return new LuaCamera(cam);
    }
}

[MoonSharpUserData]
public class LuaCFrame
{
    public Vector3 Position;
    public Quaternion Rotation;

    public LuaCFrame() { Rotation = Quaternion.identity; }
    public LuaCFrame(Vector3 position, Quaternion rotation) { Position = position; Rotation = rotation; }

    public float X { get => Position.x; set => Position.x = value; }
    public float Y { get => Position.y; set => Position.y = value; }
    public float Z { get => Position.z; set => Position.z = value; }

    public float RX { get => Rotation.eulerAngles.x * Mathf.Deg2Rad; }
    public float RY { get => Rotation.eulerAngles.y * Mathf.Deg2Rad; }
    public float RZ { get => Rotation.eulerAngles.z * Mathf.Deg2Rad; }

    public static LuaCFrame New(float x, float y, float z)
    {
        return new LuaCFrame(new Vector3(x, y, z), Quaternion.identity);
    }

    public static LuaCFrame Angles(float rx, float ry, float rz)
    {
        var q = Quaternion.Euler(rx * Mathf.Rad2Deg, ry * Mathf.Rad2Deg, rz * Mathf.Rad2Deg);
        return new LuaCFrame(Vector3.zero, q);
    }

    public static LuaCFrame operator *(LuaCFrame a, LuaCFrame b)
    {
        if (a == null) return b;
        if (b == null) return a;
        return new LuaCFrame(a.Position + a.Rotation * b.Position, a.Rotation * b.Rotation);
    }

    public override string ToString()
    {
        var e = Rotation.eulerAngles;
        return $"CFrame(pos=({Position.x}, {Position.y}, {Position.z}), euler=({e.x}, {e.y}, {e.z}))";
    }

    internal static Table BuildGlobal()
    {
        var t = new Table(LuaRunner.Instance.Lua);
        t["New"] = (System.Func<float, float, float, LuaCFrame>)New;
        t["new"] = (System.Func<float, float, float, LuaCFrame>)New;
        t["Angles"] = (System.Func<float, float, float, LuaCFrame>)Angles;
        t["fromEulerAnglesXYZ"] = (System.Func<float, float, float, LuaCFrame>)Angles;
        return t;
    }
}

[MoonSharpUserData]
public class LuaCamera
{
    private readonly Camera camera;

    public LuaCamera(Camera cam) { camera = cam; }

    public LuaCFrame CFrame
    {
        get
        {
            if (camera == null) return new LuaCFrame();
            return new LuaCFrame(camera.transform.position, camera.transform.rotation);
        }
        set
        {
            if (camera == null || value == null) return;
            camera.transform.position = value.Position;
            camera.transform.rotation = value.Rotation;
        }
    }

    public float FOV
    {
        get => camera != null ? camera.fieldOfView : 0f;
        set { if (camera != null) camera.fieldOfView = value; }
    }
}

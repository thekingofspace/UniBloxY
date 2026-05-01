using MoonSharp.Interpreter;
using UnityEngine.InputSystem;

[MoonSharpUserData]
public class LuaInputObject
{
    public string KeyCode { get; }
    public int KeyCodeId { get; }

    public LuaInputObject(string keyCode, int keyCodeId)
    {
        KeyCode = keyCode;
        KeyCodeId = keyCodeId;
    }

    public override string ToString() => $"InputObject({KeyCode}, {KeyCodeId})";
}

public class TextInputService : LuaService
{
    private Signal input;
    private LuaMouse mouse;

    public override void Register(Script script)
    {
        lua = script;
        UserData.RegisterType<LuaInputObject>();
        input = new Signal(script, "InputService.Input");
        mouse = new LuaMouse(script);
        script.Globals["InputService"] = BuildTable(script);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null)
        {
            var keys = kb.allKeys;
            for (int i = 0; i < keys.Count; i++)
            {
                var k = keys[i];
                if (k.wasPressedThisFrame)
                    input.Fire(new LuaInputObject(k.keyCode.ToString(), (int)k.keyCode), "Begin");
                if (k.wasReleasedThisFrame)
                    input.Fire(new LuaInputObject(k.keyCode.ToString(), (int)k.keyCode), "End");
            }
        }

        mouse.Tick();
    }

    private bool IsKeyDown(DynValue key)
    {
        var kb = Keyboard.current;
        if (kb == null) return false;
        var keys = kb.allKeys;

        if (key.Type == DataType.Number)
        {
            int code = (int)key.Number;
            for (int i = 0; i < keys.Count; i++)
                if ((int)keys[i].keyCode == code) return keys[i].isPressed;
            return false;
        }

        if (key.Type == DataType.String)
        {
            var name = key.String;
            for (int i = 0; i < keys.Count; i++)
                if (keys[i].keyCode.ToString() == name) return keys[i].isPressed;
        }
        return false;
    }

    private Table BuildTable(Script script)
    {
        var t = new Table(script);
        t["Input"] = input.BuildTable();
        t["IsKeyDown"] = (System.Func<DynValue, bool>)IsKeyDown;
        t["GetMouse"] = (System.Func<Table>)(() => mouse.GetTable());
        return t;
    }
}

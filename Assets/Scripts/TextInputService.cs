using MoonSharp.Interpreter;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

[MoonSharpUserData]
public class LuaInputObject
{
    public string Device { get; }
    public string KeyCode { get; }
    public int KeyCodeId { get; }
    public string ClassName => "InputObject";

    public LuaInputObject(string device, string keyCode, int keyCodeId)
    {
        Device = device;
        KeyCode = keyCode;
        KeyCodeId = keyCodeId;
    }

    public override string ToString() => $"InputObject({Device}, {KeyCode}, {KeyCodeId})";
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
        HandleKeyboard();
        HandleMouse();
        HandleGamepad();

        mouse.Tick();
    }

    void HandleKeyboard()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        var keys = kb.allKeys;

        for (int i = 0; i < keys.Count; i++)
        {
            var k = keys[i];

            if (k.wasPressedThisFrame)
                input.Fire(new LuaInputObject("Keyboard", k.keyCode.ToString(), (int)k.keyCode), "Begin");

            if (k.wasReleasedThisFrame)
                input.Fire(new LuaInputObject("Keyboard", k.keyCode.ToString(), (int)k.keyCode), "End");
        }
    }

    void HandleMouse()
    {
        var m = Mouse.current;
        if (m == null) return;

        CheckMouseButton(m.leftButton, "LeftButton", 0);
        CheckMouseButton(m.rightButton, "RightButton", 1);
        CheckMouseButton(m.middleButton, "MiddleButton", 2);
        CheckMouseButton(m.forwardButton, "ForwardButton", 3);
        CheckMouseButton(m.backButton, "BackButton", 4);
    }

    void CheckMouseButton(ButtonControl btn, string name, int id)
    {
        if (btn == null) return;

        if (btn.wasPressedThisFrame)
            input.Fire(new LuaInputObject("Mouse", name, id), "Begin");

        if (btn.wasReleasedThisFrame)
            input.Fire(new LuaInputObject("Mouse", name, id), "End");
    }

    void HandleGamepad()
    {
        var gp = Gamepad.current;
        if (gp == null) return;

        CheckPad(gp.buttonSouth, "South", 0);
        CheckPad(gp.buttonNorth, "North", 1);
        CheckPad(gp.buttonEast, "East", 2);
        CheckPad(gp.buttonWest, "West", 3);

        CheckPad(gp.leftShoulder, "LeftShoulder", 4);
        CheckPad(gp.rightShoulder, "RightShoulder", 5);

        CheckPad(gp.startButton, "Start", 6);
        CheckPad(gp.selectButton, "Select", 7);

        CheckPad(gp.leftStickButton, "LeftStick", 8);
        CheckPad(gp.rightStickButton, "RightStick", 9);

        CheckPad(gp.dpad.up, "DPadUp", 10);
        CheckPad(gp.dpad.down, "DPadDown", 11);
        CheckPad(gp.dpad.left, "DPadLeft", 12);
        CheckPad(gp.dpad.right, "DPadRight", 13);
    }

    void CheckPad(ButtonControl btn, string name, int id)
    {
        if (btn == null) return;

        if (btn.wasPressedThisFrame)
            input.Fire(new LuaInputObject("Gamepad", name, id), "Begin");

        if (btn.wasReleasedThisFrame)
            input.Fire(new LuaInputObject("Gamepad", name, id), "End");
    }

    private bool IsKeyDown(DynValue key)
    {
        var kb = Keyboard.current;
        if (kb == null) return false;

        if (key.Type == DataType.String)
        {
            var name = key.String;
            foreach (var k in kb.allKeys)
                if (k.keyCode.ToString() == name)
                    return k.isPressed;
        }

        if (key.Type == DataType.Number)
        {
            int code = (int)key.Number;
            foreach (var k in kb.allKeys)
                if ((int)k.keyCode == code)
                    return k.isPressed;
        }

        return false;
    }

    private Table BuildTable(Script script)
    {
        var t = new Table(script);

        t["Input"] = input.BuildTable();
        t["IsKeyDown"] = (System.Func<DynValue, bool>)IsKeyDown;
        t["GetMouse"] = (System.Func<Table>)(() => mouse.GetTable());

        var mt = new Table(script);
        mt["__type"] = "InputService";
        t.MetaTable = mt;

        return t;
    }
}
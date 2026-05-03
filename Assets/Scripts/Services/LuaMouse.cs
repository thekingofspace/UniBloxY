using MoonSharp.Interpreter;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class LuaMouse
{
    private readonly Script script;
    private readonly Signal clicked;
    private readonly Signal moved;
    private readonly Signal buttonDown;
    private readonly Signal buttonUp;
    private readonly Signal scrolled;
    private readonly Table table;

    private Vector2 lastPos;
    private bool positionInitialized;
    private float lastScrollY;

    public LuaMouse(Script script)
    {
        this.script = script;

        clicked = new Signal(script, "Mouse.Clicked");
        moved = new Signal(script, "Mouse.Moved");
        buttonDown = new Signal(script, "Mouse.ButtonDown");
        buttonUp = new Signal(script, "Mouse.ButtonUp");
        scrolled = new Signal(script, "Mouse.Scrolled");

        table = BuildTable();
    }

    public Table GetTable() => table;

    private Table BuildTable()
    {
        var t = new Table(script);
        t["Clicked"] = clicked.BuildTable();
        t["Moved"] = moved.BuildTable();
        t["ButtonDown"] = buttonDown.BuildTable();
        t["ButtonUp"] = buttonUp.BuildTable();
        t["Scrolled"] = scrolled.BuildTable();

        t["IsButtonDown"] = (System.Func<DynValue, string, bool>)((_, name) => IsButtonDown(name));
        t["SetLocked"] = (System.Action<DynValue, bool>)((_, locked) =>
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        });
        t["SetVisible"] = (System.Action<DynValue, bool>)((_, visible) =>
        {
            Cursor.visible = visible;
        });

        var mt = new Table(script);
        mt["__index"] = (System.Func<DynValue, DynValue, DynValue>)((_, key) =>
        {
            if (key.Type == DataType.String && key.String == "Position")
                return UserData.Create(GetPosition());
            return DynValue.Nil;
        });
        mt["__type"] = "Mouse";
        t.MetaTable = mt;

        return t;
    }

    private LuaVector2 GetPosition()
    {
        var m = Mouse.current;
        if (m == null) return new LuaVector2(0f, 0f);
        var p = m.position.ReadValue();
        return new LuaVector2(p.x, p.y);
    }

    private bool IsButtonDown(string name)
    {
        var m = Mouse.current;
        if (m == null) return false;
        switch (name)
        {
            case "Left": return m.leftButton.isPressed;
            case "Right": return m.rightButton.isPressed;
            case "Middle": return m.middleButton.isPressed;
        }
        return false;
    }

    public void Tick()
    {
        var m = Mouse.current;
        if (m == null) return;

        var pos = m.position.ReadValue();

        if (!positionInitialized)
        {
            lastPos = pos;
            positionInitialized = true;
        }
        else if (pos != lastPos)
        {
            var delta = pos - lastPos;
            lastPos = pos;
            moved.Fire(new LuaVector2(pos.x, pos.y), new LuaVector2(delta.x, delta.y));
        }

        var scroll = m.scroll.ReadValue();
        if (Mathf.Abs(scroll.y - lastScrollY) > 0.01f || scroll.x != 0f)
        {
            scrolled.Fire(scroll.x, scroll.y);
            lastScrollY = scroll.y;
        }

        CheckButton(m.leftButton, "Left");
        CheckButton(m.rightButton, "Right");
        CheckButton(m.middleButton, "Middle");
    }

    private void CheckButton(ButtonControl b, string name)
    {
        if (b == null) return;

        if (b.wasPressedThisFrame) buttonDown.Fire(name);
        if (b.wasReleasedThisFrame)
        {
            buttonUp.Fire(name);
            clicked.Fire(name);
        }
    }
}
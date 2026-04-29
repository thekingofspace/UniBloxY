using System.Collections.Generic;
using MoonSharp.Interpreter;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

[MoonSharpUserData]
public class LuaInputObject
{
    public string KeyName { get; }
    public int KeyCode { get; }

    public LuaInputObject(string keyName, int keyCode)
    {
        KeyName = keyName;
        KeyCode = keyCode;
    }

    public override string ToString() => $"InputObject({KeyName}, {KeyCode})";
}

[MoonSharpUserData]
public class LuaVector2
{
    public float X { get; set; }
    public float Y { get; set; }

    public LuaVector2(float x, float y) { X = x; Y = y; }

    public override string ToString() => $"({X}, {Y})";
}

[MoonSharpUserData]
public class LuaMouse
{
    private readonly InputGetter owner;

    public LuaMouse(InputGetter o) { owner = o; }

    private static Vector2 RawPosition()
    {
        var m = Mouse.current;
        return m != null ? m.position.ReadValue() : Vector2.zero;
    }

    private static Vector2 RawDelta()
    {
        var m = Mouse.current;
        return m != null ? m.delta.ReadValue() : Vector2.zero;
    }

    public float X => RawPosition().x;
    public float Y => RawPosition().y;

    public LuaVector2 AbsolutePosition
    {
        get { var p = RawPosition(); return new LuaVector2(p.x, p.y); }
    }

    public LuaVector2 Delta
    {
        get { var d = RawDelta(); return new LuaVector2(d.x, d.y); }
    }

    public bool Locked
    {
        get => Cursor.lockState == CursorLockMode.Locked;
        set
        {
            Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !value;
        }
    }

    public DynValue OnMove(Closure callback) => owner.RegisterOnMove(callback);
}

public class InputGetter : MonoBehaviour
{
    private class Binding
    {
        public int Id;
        public Closure Callback;
    }

    private readonly List<Binding> inputBindings = new List<Binding>();
    private readonly List<Binding> moveBindings = new List<Binding>();
    private int nextId;

    private LuaMouse mouseInstance;

    private Script Lua => LuaRunner.Instance.Lua;

    public void Initialize()
    {
        UserData.RegisterType<LuaInputObject>();
        UserData.RegisterType<LuaMouse>();
        UserData.RegisterType<LuaVector2>();

        mouseInstance = new LuaMouse(this);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        var t = new Table(Lua);
        t["OnInput"] = (System.Func<Closure, DynValue>)RegisterOnInput;
        t["GetMouse"] = (System.Func<LuaMouse>)(() => mouseInstance);
        t["SetMouseLocked"] = (System.Action<bool>)(v => mouseInstance.Locked = v);
        Lua.Globals["UserInput"] = t;
    }

    public DynValue RegisterOnInput(Closure callback)
    {
        return AddBinding(inputBindings, callback, "OnInput");
    }

    public DynValue RegisterOnMove(Closure callback)
    {
        return AddBinding(moveBindings, callback, "Mouse.OnMove");
    }

    private DynValue AddBinding(List<Binding> list, Closure callback, string label)
    {
        if (callback == null)
        {
            Debug.LogError($"UserInput.{label}: callback is nil");
            return DynValue.Nil;
        }

        var b = new Binding { Id = ++nextId, Callback = callback };
        list.Add(b);

        System.Action unbind = () =>
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Id == b.Id) { list.RemoveAt(i); return; }
            }
        };
        return DynValue.FromObject(Lua, unbind);
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
                    FireInput(new LuaInputObject(k.keyCode.ToString(), (int)k.keyCode), "Input");
                if (k.wasReleasedThisFrame)
                    FireInput(new LuaInputObject(k.keyCode.ToString(), (int)k.keyCode), "Release");
            }
        }

        var mouse = Mouse.current;
        if (mouse != null)
        {
            CheckButton(mouse.leftButton, "MouseButton1", 1001);
            CheckButton(mouse.rightButton, "MouseButton2", 1002);
            CheckButton(mouse.middleButton, "MouseButton3", 1003);

            var delta = mouse.delta.ReadValue();
            if (delta.x != 0f || delta.y != 0f)
                FireMove(delta);
        }
    }

    private void CheckButton(ButtonControl btn, string name, int code)
    {
        if (btn == null) return;
        if (btn.wasPressedThisFrame)
            FireInput(new LuaInputObject(name, code), "Input");
        if (btn.wasReleasedThisFrame)
            FireInput(new LuaInputObject(name, code), "Release");
    }

    private void FireInput(LuaInputObject obj, string state)
    {
        if (inputBindings.Count == 0) return;
        var snapshot = inputBindings.ToArray();
        for (int i = 0; i < snapshot.Length; i++)
        {
            try { Lua.Call(snapshot[i].Callback, obj, state); }
            catch (ScriptRuntimeException ex)
            {
                Debug.LogError($"UserInput.OnInput error: {ex.DecoratedMessage}");
            }
        }
    }

    private void FireMove(Vector2 pos)
    {
        if (moveBindings.Count == 0) return;
        var snapshot = moveBindings.ToArray();
        var vec = new LuaVector2(pos.x, pos.y);
        for (int i = 0; i < snapshot.Length; i++)
        {
            try { Lua.Call(snapshot[i].Callback, vec); }
            catch (ScriptRuntimeException ex)
            {
                Debug.LogError($"Mouse.OnMove error: {ex.DecoratedMessage}");
            }
        }
    }
}

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

    private static string currentCursorName = "Default";
    private static readonly System.Collections.Generic.Dictionary<string, (Texture2D tex, Vector2 hotspot)> cursorCache = new();

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

        t["IsButtonDown"] = DynValue.NewCallback((ctx, args) =>
        {

            var name = args.Count > 0 && args[args.Count - 1].Type == DataType.String
                ? args[args.Count - 1].String : null;
            return DynValue.NewBoolean(IsButtonDown(name));
        });
        t["SetLocked"] = DynValue.NewCallback((ctx, args) =>
        {
            var locked = args.Count > 0 && args[args.Count - 1].CastToBool();
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            return DynValue.Nil;
        });
        t["SetVisible"] = DynValue.NewCallback((ctx, args) =>
        {
            var visible = args.Count > 0 && args[args.Count - 1].CastToBool();
            Cursor.visible = visible;
            return DynValue.Nil;
        });

        t["SetCursor"] = DynValue.NewCallback((ctx, args) =>
        {

            int start = (args.Count > 0 && args[0].Type == DataType.Table) ? 1 : 0;
            var first = args.Count > start ? args[start] : DynValue.Nil;
            var second = args.Count > start + 1 ? args[start + 1] : DynValue.Nil;
            ApplyCursor(first, second);
            return DynValue.Nil;
        });
        t["SetCursorType"] = DynValue.NewCallback((ctx, args) =>
        {
            int start = (args.Count > 0 && args[0].Type == DataType.Table) ? 1 : 0;
            var first = args.Count > start ? args[start] : DynValue.Nil;
            var second = args.Count > start + 1 ? args[start + 1] : DynValue.Nil;
            ApplyCursor(first, second);
            return DynValue.Nil;
        });
        t["ResetCursor"] = DynValue.NewCallback((ctx, args) =>
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            currentCursorName = "Default";
            return DynValue.Nil;
        });

        var mt = new Table(script);
        mt["__index"] = (System.Func<DynValue, DynValue, DynValue>)((_, key) =>
        {
            if (key.Type != DataType.String) return DynValue.Nil;
            switch (key.String)
            {
                case "Position": return UserData.Create(GetPosition());
                case "Cursor":
                case "CursorType": return DynValue.NewString(currentCursorName);
            }
            return DynValue.Nil;
        });

        mt["__newindex"] = DynValue.NewCallback((ctx, args) =>
        {
            var self = args.Count > 0 ? args[0] : DynValue.Nil;
            var key = args.Count > 1 ? args[1] : DynValue.Nil;
            var value = args.Count > 2 ? args[2] : DynValue.Nil;
            if (key.Type == DataType.String &&
                (key.String == "Cursor" || key.String == "CursorType"))
            {
                ApplyCursor(value, DynValue.Nil);
                return DynValue.Nil;
            }
            if (self.Type == DataType.Table)
                self.Table.Set(key, value);
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
        // Read delta directly so mouse-look keeps working when the cursor is
        // locked (in Locked mode position freezes but delta still reports motion).
        var delta = m.delta.ReadValue();

        if (!positionInitialized)
        {
            lastPos = pos;
            positionInitialized = true;
        }
        else if (delta.x != 0f || delta.y != 0f)
        {
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

    private static void ApplyCursor(DynValue value, DynValue hotspotArg)
    {

        Vector2? hotspot = null;
        if (hotspotArg.Type == DataType.UserData && hotspotArg.UserData.Object is LuaVector2 v2)
            hotspot = new Vector2(v2.X, v2.Y);

        if (value.IsNil())
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            currentCursorName = "Default";
            return;
        }

        if (value.Type == DataType.String)
        {
            var name = value.String;
            var tex = ResolveNamedCursor(name, out var defaultHotspot);

            if (tex == null)
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
            else
            {
                Cursor.SetCursor(tex, hotspot ?? defaultHotspot, CursorMode.Auto);
            }
            currentCursorName = string.IsNullOrEmpty(name) ? "Default" : name;
            return;
        }

        if (value.Type == DataType.UserData)
        {
            Texture2D tex = null;
            string label = "Custom";
            switch (value.UserData.Object)
            {
                case LuaImage li:
                    tex = li.Texture as Texture2D ?? (li.Sprite != null ? li.Sprite.texture as Texture2D : null);
                    label = string.IsNullOrEmpty(li.Name) ? "Custom" : li.Name;
                    break;
                case LuaTexture lt:
                    tex = lt.Texture as Texture2D;
                    label = string.IsNullOrEmpty(lt.Name) ? "Custom" : lt.Name;
                    break;
                case Texture2D bare:
                    tex = bare;
                    break;
            }

            if (tex == null)
                throw new ScriptRuntimeException("Mouse:SetCursor expected a string, Image, Texture, or nil");

            var hp = hotspot ?? new Vector2(tex.width / 2f, tex.height / 2f);
            Cursor.SetCursor(tex, hp, CursorMode.Auto);
            currentCursorName = label;
            return;
        }

        throw new ScriptRuntimeException("Mouse:SetCursor expected a string, Image, Texture, or nil");
    }

    private static Texture2D ResolveNamedCursor(string name, out Vector2 hotspot)
    {
        hotspot = Vector2.zero;
        if (string.IsNullOrEmpty(name)) return null;
        var canonical = name.Trim().ToLowerInvariant();

        if (canonical == "default" || canonical == "arrow" || canonical == "system")
            return null;

        if (cursorCache.TryGetValue(canonical, out var entry))
        {
            hotspot = entry.hotspot;
            return entry.tex;
        }

        var fromRes = Resources.Load<Texture2D>("Cursors/" + name);
        Texture2D tex;
        Vector2 hp;
        if (fromRes != null)
        {
            tex = fromRes;
            hp = new Vector2(tex.width / 2f, tex.height / 2f);
        }
        else
        {
            switch (canonical)
            {
                case "crosshair":
                case "cross":
                    tex = BuildCrosshair(out hp); break;
                case "ibeam":
                case "text":
                case "caret":
                    tex = BuildIBeam(out hp); break;
                case "wait":
                case "busy":
                case "hourglass":
                    tex = BuildHourglass(out hp); break;
                case "hand":
                case "pointer":
                    tex = BuildPointer(out hp); break;
                default:
                    Debug.LogWarning($"Mouse:SetCursor: unknown cursor type \"{name}\" — falling back to system default");
                    return null;
            }
        }

        cursorCache[canonical] = (tex, hp);
        hotspot = hp;
        return tex;
    }

    private const int CursorSize = 16;

    private static Texture2D NewCursorTexture()
    {
        var t = new Texture2D(CursorSize, CursorSize, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Point;
        t.wrapMode = TextureWrapMode.Clamp;
        var clear = new Color[CursorSize * CursorSize];
        t.SetPixels(clear);
        return t;
    }

    private static void Plot(Texture2D t, int x, int y, Color c)
    {
        if (x < 0 || y < 0 || x >= t.width || y >= t.height) return;
        t.SetPixel(x, t.height - 1 - y, c);
    }

    private static void DrawHLine(Texture2D t, int x0, int x1, int y, Color c)
    {
        if (x0 > x1) (x0, x1) = (x1, x0);
        for (int x = x0; x <= x1; x++) Plot(t, x, y, c);
    }

    private static void DrawVLine(Texture2D t, int x, int y0, int y1, Color c)
    {
        if (y0 > y1) (y0, y1) = (y1, y0);
        for (int y = y0; y <= y1; y++) Plot(t, x, y, c);
    }

    private static Texture2D BuildCrosshair(out Vector2 hotspot)
    {
        var t = NewCursorTexture();
        var black = Color.black;
        var white = Color.white;

        DrawVLine(t, 8, 0, 6, black);
        DrawVLine(t, 8, 9, 15, black);
        DrawHLine(t, 0, 6, 8, black);
        DrawHLine(t, 9, 15, 8, black);

        DrawVLine(t, 7, 0, 6, white); DrawVLine(t, 9, 0, 6, white);
        DrawVLine(t, 7, 9, 15, white); DrawVLine(t, 9, 9, 15, white);
        DrawHLine(t, 0, 6, 7, white);  DrawHLine(t, 0, 6, 9, white);
        DrawHLine(t, 9, 15, 7, white); DrawHLine(t, 9, 15, 9, white);

        Plot(t, 8, 8, white);
        t.Apply();
        hotspot = new Vector2(8, 8);
        return t;
    }

    private static Texture2D BuildIBeam(out Vector2 hotspot)
    {
        var t = NewCursorTexture();
        var black = Color.black;
        var white = Color.white;

        DrawVLine(t, 7, 2, 13, black);
        DrawVLine(t, 8, 2, 13, black);
        DrawVLine(t, 6, 2, 13, white);
        DrawVLine(t, 9, 2, 13, white);

        DrawHLine(t, 5, 10, 2, black);
        DrawHLine(t, 5, 10, 13, black);
        DrawHLine(t, 5, 10, 1, white);
        DrawHLine(t, 5, 10, 14, white);
        t.Apply();
        hotspot = new Vector2(8, 8);
        return t;
    }

    private static Texture2D BuildHourglass(out Vector2 hotspot)
    {
        var t = NewCursorTexture();
        var black = Color.black;

        DrawHLine(t, 4, 11, 1, black);
        DrawHLine(t, 4, 11, 14, black);

        for (int y = 2; y <= 7; y++)
        {
            int inset = y - 2;
            Plot(t, 4 + inset, y, black);
            Plot(t, 11 - inset, y, black);
        }

        for (int y = 8; y <= 13; y++)
        {
            int inset = 13 - y;
            Plot(t, 4 + inset, y, black);
            Plot(t, 11 - inset, y, black);
        }

        Plot(t, 7, 7, black); Plot(t, 8, 7, black);
        Plot(t, 7, 8, black); Plot(t, 8, 8, black);
        t.Apply();
        hotspot = new Vector2(8, 8);
        return t;
    }

    private static Texture2D BuildPointer(out Vector2 hotspot)
    {
        var t = NewCursorTexture();
        var black = Color.black;
        var white = Color.white;

        Plot(t, 8, 1, black);
        DrawHLine(t, 7, 9, 2, black);
        DrawHLine(t, 6, 10, 3, black);
        DrawHLine(t, 5, 11, 4, black);
        DrawHLine(t, 4, 12, 5, black);

        DrawVLine(t, 7, 6, 13, black);
        DrawVLine(t, 8, 6, 13, black);
        DrawVLine(t, 9, 6, 13, black);

        Plot(t, 8, 0, white);
        Plot(t, 7, 1, white); Plot(t, 9, 1, white);
        Plot(t, 6, 2, white); Plot(t, 10, 2, white);
        Plot(t, 5, 3, white); Plot(t, 11, 3, white);
        Plot(t, 4, 4, white); Plot(t, 12, 4, white);
        Plot(t, 3, 5, white); Plot(t, 13, 5, white);
        Plot(t, 6, 6, white); Plot(t, 10, 6, white);
        DrawVLine(t, 6, 6, 14, white);
        DrawVLine(t, 10, 6, 14, white);
        DrawHLine(t, 7, 9, 14, white);
        t.Apply();
        hotspot = new Vector2(8, 1);
        return t;
    }
}
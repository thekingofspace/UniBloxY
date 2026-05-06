using MoonSharp.Interpreter;
using UnityEngine;
using UnityEngine.UI;

public class TextLabel : ShadableUI
{
    public override string ClassName => "TextLabel";

    public override bool Clonable => true;

    private class State
    {
        public LuaUDim2 Size = GUIRect.DefaultSize;
        public LuaUDim2 Position = GUIRect.DefaultPosition;
        public string Text = "Label";
        public LuaFont Font;
        public int TextSize = 14;
        public LuaColor3 TextColor = new LuaColor3(1f, 1f, 1f);
        public float TextTransparency = 0f;
        public bool TextScaled = false;
        public string XAlign = "Left";
        public string YAlign = "Top";
        public Text TextComp;
    }

    public override void Initialize(LuaInstance instance)
    {
        instance.UserState = new State();
    }

    public override void CopyState(LuaInstance source, LuaInstance target)
    {
        base.CopyState(source, target);
        if (source.UserState is State s && target.UserState is State d)
        {
            d.Size = s.Size;
            d.Position = s.Position;
            d.Text = s.Text;
            d.Font = s.Font;
            d.TextSize = s.TextSize;
            d.TextColor = s.TextColor;
            d.TextTransparency = s.TextTransparency;
            d.TextScaled = s.TextScaled;
            d.XAlign = s.XAlign;
            d.YAlign = s.YAlign;
        }
    }

    public override void OnEnterScene(LuaInstance instance)
    {
        if (instance.UnityObject != null) return;
        var s = (State)instance.UserState;

        var go = new GameObject(instance.Name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        s.TextComp = go.AddComponent<Text>();
        if (s.Font == null)
        {

            var fallback = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                        ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            s.TextComp.font = fallback;
        }
        instance.UnityObject = go;

        OnUnityObjectCreated(instance);
        ApplyText(s);
        GUIRect.Apply(rt, s.Size, s.Position, GetAnchorPoint(instance));
        ApplyMaterial(instance);
    }

    public override void OnExitScene(LuaInstance instance)
    {
        if (instance.UnityObject != null)
        {
            Object.Destroy(instance.UnityObject);
            instance.UnityObject = null;
        }
    }

    public override void ImportFromUnityObject(LuaInstance instance, GameObject go)
    {
        var s = (State)instance.UserState;
        var rt = go.GetComponent<RectTransform>();
        if (rt == null)
            throw new ScriptRuntimeException(
                $"TextLabel import: GameObject \"{go.name}\" has no RectTransform — only UI objects can be wrapped as TextLabel");

        s.TextComp = go.GetComponent<Text>() ?? go.AddComponent<Text>();
        if (s.TextComp.font == null)
        {
            var fb = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                  ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            s.TextComp.font = fb;
        }

        var size = rt.rect.size;
        s.Size = new LuaUDim2(new LuaUDim(0f, size.x), new LuaUDim(0f, size.y));
        var ap = rt.anchoredPosition;
        s.Position = new LuaUDim2(new LuaUDim(0f, ap.x), new LuaUDim(0f, -ap.y));

        s.Text = s.TextComp.text ?? "";
        if (s.TextComp.font != null)
            s.Font = new LuaFont(s.TextComp.font.name ?? "", s.TextComp.font);
        s.TextSize = s.TextComp.fontSize;
        var c = s.TextComp.color;
        s.TextColor = new LuaColor3(c.r, c.g, c.b);
        s.TextTransparency = 1f - c.a;
        s.TextScaled = s.TextComp.resizeTextForBestFit;
        SplitAnchor(s.TextComp.alignment, out s.XAlign, out s.YAlign);

        StoreAnchorPoint(instance, new LuaVector2(rt.pivot.x, 1f - rt.pivot.y));
    }

    protected override void ReapplyRect(LuaInstance instance)
    {
        if (instance.UnityObject == null) return;
        var s = (State)instance.UserState;
        GUIRect.Apply((RectTransform)instance.UnityObject.transform, s.Size, s.Position, GetAnchorPoint(instance));
    }

    protected override void SetPosition(LuaInstance instance, LuaUDim2 position)
    {
        ((State)instance.UserState).Position = position;
    }

    private static void SplitAnchor(TextAnchor a, out string x, out string y)
    {
        switch (a)
        {
            case TextAnchor.UpperLeft:    x = "Left";   y = "Top";    break;
            case TextAnchor.UpperCenter:  x = "Center"; y = "Top";    break;
            case TextAnchor.UpperRight:   x = "Right";  y = "Top";    break;
            case TextAnchor.MiddleLeft:   x = "Left";   y = "Center"; break;
            case TextAnchor.MiddleCenter: x = "Center"; y = "Center"; break;
            case TextAnchor.MiddleRight:  x = "Right";  y = "Center"; break;
            case TextAnchor.LowerLeft:    x = "Left";   y = "Bottom"; break;
            case TextAnchor.LowerCenter:  x = "Center"; y = "Bottom"; break;
            case TextAnchor.LowerRight:   x = "Right";  y = "Bottom"; break;
            default:                      x = "Left";   y = "Top";    break;
        }
    }

    public override bool TryGetProperty(LuaInstance instance, string key, out DynValue value)
    {
        var s = (State)instance.UserState;
        switch (key)
        {
            case "Size": value = UserData.Create(s.Size); return true;
            case "Position": value = UserData.Create(s.Position); return true;
            case "Text": value = DynValue.NewString(s.Text); return true;
            case "Font": value = s.Font != null ? UserData.Create(s.Font) : DynValue.Nil; return true;
            case "TextSize": value = DynValue.NewNumber(s.TextSize); return true;
            case "TextColor":
            case "TextColor3":
                value = UserData.Create(s.TextColor); return true;
            case "TextTransparency": value = DynValue.NewNumber(s.TextTransparency); return true;
            case "TextScaled": value = DynValue.NewBoolean(s.TextScaled); return true;
            case "TextXAlignment": value = DynValue.NewString(s.XAlign); return true;
            case "TextYAlignment": value = DynValue.NewString(s.YAlign); return true;
            case "TextAlignment":
                value = DynValue.NewString(JoinAlignment(s.XAlign, s.YAlign));
                return true;
        }
        return base.TryGetProperty(instance, key, out value);
    }

    public override bool TrySetProperty(LuaInstance instance, string key, DynValue value)
    {
        var s = (State)instance.UserState;
        switch (key)
        {
            case "Size":
                s.Size = GUIRect.ResolveUDim2(value, "Size");
                if (instance.UnityObject != null)
                    GUIRect.Apply((RectTransform)instance.UnityObject.transform, s.Size, s.Position, GetAnchorPoint(instance));
                return true;
            case "Position":
                s.Position = GUIRect.ResolveUDim2(value, "Position");
                if (instance.UnityObject != null)
                    GUIRect.Apply((RectTransform)instance.UnityObject.transform, s.Size, s.Position, GetAnchorPoint(instance));
                return true;
            case "Text":
                if (value.Type != DataType.String)
                    throw new ScriptRuntimeException("Text must be a string");
                s.Text = value.String;
                ApplyText(s);
                return true;
            case "Font":
                if (value.IsNil()) { s.Font = null; ApplyText(s); return true; }
                if (value.Type == DataType.UserData && value.UserData.Object is LuaFont f)
                {
                    s.Font = f;
                    ApplyText(s);
                    return true;
                }
                throw new ScriptRuntimeException("Font must be a Font from AssetService:GetFont");
            case "TextSize":
                if (value.Type != DataType.Number)
                    throw new ScriptRuntimeException("TextSize must be a number");
                s.TextSize = (int)value.Number;
                ApplyText(s);
                return true;
            case "TextColor":
            case "TextColor3":
                s.TextColor = GUIRect.ResolveColor3(value, "TextColor");
                ApplyText(s);
                return true;
            case "TextTransparency":
                if (value.Type != DataType.Number)
                    throw new ScriptRuntimeException("TextTransparency must be a number");
                s.TextTransparency = Mathf.Clamp01((float)value.Number);
                ApplyText(s);
                return true;
            case "TextScaled":
                if (value.Type != DataType.Boolean)
                    throw new ScriptRuntimeException("TextScaled must be a boolean");
                s.TextScaled = value.Boolean;
                ApplyText(s);
                return true;
            case "TextXAlignment":
                if (value.Type != DataType.String)
                    throw new ScriptRuntimeException("TextXAlignment must be a string");
                s.XAlign = value.String;
                ApplyText(s);
                return true;
            case "TextYAlignment":
                if (value.Type != DataType.String)
                    throw new ScriptRuntimeException("TextYAlignment must be a string");
                s.YAlign = value.String;
                ApplyText(s);
                return true;
            case "TextAlignment":
                if (value.Type != DataType.String)
                    throw new ScriptRuntimeException("TextAlignment must be a string");
                if (!ResolveTextAlignment(value.String, out var ax, out var ay))
                    throw new ScriptRuntimeException(
                        $"TextAlignment \"{value.String}\" is not recognized (TopLeft/Top/TopRight/Left/Center/Right/BottomLeft/Bottom/BottomRight)");
                s.XAlign = ax;
                s.YAlign = ay;
                ApplyText(s);
                return true;
        }
        return base.TrySetProperty(instance, key, value);
    }

    private static string JoinAlignment(string xAlign, string yAlign)
    {
        var x = (xAlign ?? "").ToLowerInvariant();
        var y = (yAlign ?? "").ToLowerInvariant();
        if (y == "top")    { if (x == "left") return "TopLeft";    if (x == "center") return "Top";    if (x == "right") return "TopRight"; }
        if (y == "center") { if (x == "left") return "Left";       if (x == "center") return "Center"; if (x == "right") return "Right"; }
        if (y == "bottom") { if (x == "left") return "BottomLeft"; if (x == "center") return "Bottom"; if (x == "right") return "BottomRight"; }
        return $"{xAlign}-{yAlign}";
    }

    private static bool ResolveTextAlignment(string name, out string xAlign, out string yAlign)
    {
        xAlign = "Left"; yAlign = "Top";
        if (string.IsNullOrEmpty(name)) return false;
        switch (name.Trim().ToLowerInvariant())
        {
            case "topleft":     xAlign = "Left";   yAlign = "Top";    return true;
            case "top":         xAlign = "Center"; yAlign = "Top";    return true;
            case "topright":    xAlign = "Right";  yAlign = "Top";    return true;
            case "left":        xAlign = "Left";   yAlign = "Center"; return true;
            case "center":
            case "middle":      xAlign = "Center"; yAlign = "Center"; return true;
            case "right":       xAlign = "Right";  yAlign = "Center"; return true;
            case "bottomleft":  xAlign = "Left";   yAlign = "Bottom"; return true;
            case "bottom":      xAlign = "Center"; yAlign = "Bottom"; return true;
            case "bottomright": xAlign = "Right";  yAlign = "Bottom"; return true;
        }
        return false;
    }

    private static void ApplyText(State s)
    {
        if (s.TextComp == null) return;
        s.TextComp.text = s.Text;
        if (s.Font?.Font != null) s.TextComp.font = s.Font.Font;
        s.TextComp.fontSize = s.TextSize;
        s.TextComp.color = new Color(
            s.TextColor.R, s.TextColor.G, s.TextColor.B,
            1f - s.TextTransparency);

        if (s.TextScaled)
        {
            s.TextComp.resizeTextForBestFit = true;
            s.TextComp.resizeTextMinSize = 1;
            s.TextComp.resizeTextMaxSize = 300;
        }
        else
        {
            s.TextComp.resizeTextForBestFit = false;
        }

        s.TextComp.alignment = ResolveAnchor(s.XAlign, s.YAlign);
    }

    private static TextAnchor ResolveAnchor(string x, string y)
    {
        var xl = (x ?? "").ToLowerInvariant();
        var yl = (y ?? "").ToLowerInvariant();

        bool top = yl == "top";
        bool bottom = yl == "bottom";

        bool left = xl == "left";
        bool right = xl == "right";

        if (top)    return left ? TextAnchor.UpperLeft   : right ? TextAnchor.UpperRight   : TextAnchor.UpperCenter;
        if (bottom) return left ? TextAnchor.LowerLeft   : right ? TextAnchor.LowerRight   : TextAnchor.LowerCenter;
        return        left ? TextAnchor.MiddleLeft  : right ? TextAnchor.MiddleRight  : TextAnchor.MiddleCenter;
    }
}

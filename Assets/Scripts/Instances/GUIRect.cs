using MoonSharp.Interpreter;
using UnityEngine;

// Shared Size/Position handling for ShadableUI subclasses. Sizes/positions are
// LuaUDim2 values mapping to RectTransform anchor + offset pairs the way
// Roblox-style "{Scale, Offset}" does — anchorMin/Max collapse to a single
// point and offsets carry the pixel deltas.
internal static class GUIRect
{
    public static readonly LuaUDim2 DefaultSize =
        new LuaUDim2(new LuaUDim(0f, 100f), new LuaUDim(0f, 100f));
    public static readonly LuaUDim2 DefaultPosition =
        new LuaUDim2(new LuaUDim(0f, 0f), new LuaUDim(0f, 0f));

    public static void Apply(RectTransform rt, LuaUDim2 size, LuaUDim2 position, LuaVector2 anchorPoint)
    {
        if (rt == null) return;

        var anchorX = position.X.Scale;
        var anchorY = 1f - position.Y.Scale;
        rt.anchorMin = new Vector2(anchorX, anchorY);
        rt.anchorMax = new Vector2(anchorX, anchorY);

        // Roblox AnchorPoint Y=0 means top, Y=1 means bottom — UGUI's pivot is
        // bottom-up, so flip Y. Default (0,0) keeps the legacy top-left pivot.
        var ap = anchorPoint ?? LuaVector2.Zero;
        rt.pivot = new Vector2(ap.X, 1f - ap.Y);

        rt.sizeDelta = new Vector2(
            size.X.Scale != 0f ? size.X.Offset + size.X.Scale * GetParentWidth(rt)  : size.X.Offset,
            size.Y.Scale != 0f ? size.Y.Offset + size.Y.Scale * GetParentHeight(rt) : size.Y.Offset);

        rt.anchoredPosition = new Vector2(position.X.Offset, -position.Y.Offset);
    }

    private static float GetParentWidth(RectTransform rt)
    {
        var p = rt.parent as RectTransform;
        return p != null ? p.rect.width : 0f;
    }

    private static float GetParentHeight(RectTransform rt)
    {
        var p = rt.parent as RectTransform;
        return p != null ? p.rect.height : 0f;
    }

    public static LuaUDim2 ResolveUDim2(DynValue v, string propName)
    {
        if (v.Type == DataType.UserData && v.UserData.Object is LuaUDim2 u) return u;
        throw new ScriptRuntimeException($"{propName} must be a UDim2");
    }

    public static LuaColor3 ResolveColor3(DynValue v, string propName)
    {
        if (v.Type == DataType.UserData && v.UserData.Object is LuaColor3 c) return c;
        throw new ScriptRuntimeException($"{propName} must be a Color3");
    }
}

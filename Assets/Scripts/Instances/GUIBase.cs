using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MoonSharp.Interpreter;
using UnityEngine;
using UnityEngine.UI;

public abstract class GUIBase : LuaInstanceClass
{
    private class GUIData
    {
        public bool Visible = true;
        public int ZIndex = 0;
        public LuaVector2 AnchorPoint = LuaVector2.Zero;
        // Last Placement preset the user assigned. "Custom" means none / mixed
        // (i.e. AnchorPoint or Position were set directly). The string is stored
        // so reading Placement back is meaningful.
        public string Placement = "Custom";
    }

    private static readonly ConditionalWeakTable<LuaInstance, GUIData> data = new();
    private static GUIData Get(LuaInstance instance) =>
        data.GetValue(instance, _ => new GUIData());

    // We do our own Unity reparenting so a GUI without a UI ancestor lands
    // under the auto-created root Canvas instead of the parent's plain GameObject.
    public override bool ParentsUnityObject => false;

    public bool GetVisible(LuaInstance instance) => Get(instance).Visible;
    public int GetZIndex(LuaInstance instance) => Get(instance).ZIndex;
    public static LuaVector2 GetAnchorPoint(LuaInstance instance) => Get(instance).AnchorPoint;

    // ImportFromUnityObject in subclasses needs to seed AnchorPoint from the
    // imported RectTransform's pivot.
    protected static void StoreAnchorPoint(LuaInstance instance, LuaVector2 ap) =>
        Get(instance).AnchorPoint = ap ?? LuaVector2.Zero;

    // Subclasses owning Size/Position state override this to re-run GUIRect.Apply
    // when something cross-cutting (currently AnchorPoint) changes.
    protected virtual void ReapplyRect(LuaInstance instance) {}

    // Subclasses override to write into their state.Position. Used by Placement
    // presets so the GUIBase layer can update both AnchorPoint and Position
    // without knowing each subclass's internal State layout.
    protected virtual void SetPosition(LuaInstance instance, LuaUDim2 position) {}

    public static bool EffectiveVisible(LuaInstance instance)
    {
        var node = instance;
        while (node != null)
        {
            if (node.ClassDef is GUIBase g && !g.GetVisible(node)) return false;
            node = node.Parent;
        }
        return true;
    }

    public override void CopyState(LuaInstance source, LuaInstance target)
    {
        var s = Get(source);
        var d = Get(target);
        d.Visible = s.Visible;
        d.ZIndex = s.ZIndex;
        d.AnchorPoint = s.AnchorPoint;
        d.Placement = s.Placement;
    }

    public override void OnAncestryChanged(LuaInstance instance)
    {
        Reparent(instance);
        ApplyEffectiveVisible(instance);
    }

    public override bool TryGetProperty(LuaInstance instance, string key, out DynValue value)
    {
        switch (key)
        {
            case "Visible": value = DynValue.NewBoolean(Get(instance).Visible); return true;
            case "ZIndex": value = DynValue.NewNumber(Get(instance).ZIndex); return true;
            case "AnchorPoint": value = UserData.Create(Get(instance).AnchorPoint); return true;
            case "Placement": value = DynValue.NewString(Get(instance).Placement); return true;
        }
        value = DynValue.Nil;
        return false;
    }

    public override bool TrySetProperty(LuaInstance instance, string key, DynValue value)
    {
        switch (key)
        {
            case "Visible":
                if (value.Type != DataType.Boolean)
                    throw new ScriptRuntimeException("Visible must be a boolean");
                Get(instance).Visible = value.Boolean;
                ApplyEffectiveVisible(instance);
                return true;
            case "ZIndex":
                if (value.Type != DataType.Number)
                    throw new ScriptRuntimeException("ZIndex must be a number");
                Get(instance).ZIndex = (int)value.Number;
                ApplyZIndex(instance);
                return true;
            case "AnchorPoint":
                if (value.Type == DataType.UserData && value.UserData.Object is LuaVector2 ap)
                {
                    Get(instance).AnchorPoint = ap;
                    Get(instance).Placement = "Custom";
                    ReapplyRect(instance);
                    return true;
                }
                throw new ScriptRuntimeException("AnchorPoint must be a Vector2");
            case "Placement":
                if (value.Type != DataType.String)
                    throw new ScriptRuntimeException("Placement must be a string");
                if (!ResolvePlacement(value.String, out var sx, out var sy, out var canonical))
                    throw new ScriptRuntimeException(
                        $"Placement \"{value.String}\" is not a recognized preset (TopLeft/Top/TopRight/Left/Center/Right/BottomLeft/Bottom/BottomRight)");
                Get(instance).AnchorPoint = new LuaVector2(sx, sy);
                Get(instance).Placement = canonical;
                SetPosition(instance, new LuaUDim2(new LuaUDim(sx, 0f), new LuaUDim(sy, 0f)));
                ReapplyRect(instance);
                return true;
        }
        return false;
    }

    // Maps a preset name to the (Position scale, AnchorPoint) pair. Both share
    // the same scale values: a TopLeft anchor sits at the parent's top-left,
    // a Center anchor sits at the parent's center, etc.
    private static bool ResolvePlacement(string name, out float x, out float y, out string canonical)
    {
        x = 0f; y = 0f; canonical = null;
        if (string.IsNullOrEmpty(name)) return false;
        switch (name.Trim().ToLowerInvariant())
        {
            case "topleft":     x = 0f;   y = 0f;   canonical = "TopLeft";     return true;
            case "top":         x = 0.5f; y = 0f;   canonical = "Top";         return true;
            case "topright":    x = 1f;   y = 0f;   canonical = "TopRight";    return true;
            case "left":        x = 0f;   y = 0.5f; canonical = "Left";        return true;
            case "center":
            case "middle":      x = 0.5f; y = 0.5f; canonical = "Center";      return true;
            case "right":       x = 1f;   y = 0.5f; canonical = "Right";       return true;
            case "bottomleft":  x = 0f;   y = 1f;   canonical = "BottomLeft";  return true;
            case "bottom":      x = 0.5f; y = 1f;   canonical = "Bottom";      return true;
            case "bottomright": x = 1f;   y = 1f;   canonical = "BottomRight"; return true;
        }
        return false;
    }

    protected static void ApplyEffectiveVisible(LuaInstance node)
    {
        if (node.ClassDef is GUIBase)
        {
            var go = node.UnityObject;
            if (go != null) go.SetActive(EffectiveVisible(node));
        }
        for (int i = 0; i < node.Children.Count; i++)
            ApplyEffectiveVisible(node.Children[i]);
    }

    protected static void Reparent(LuaInstance instance)
    {
        if (instance.UnityObject == null) return;
        var t = instance.UnityObject.transform;
        var parent = FindUIParent(instance);
        t.SetParent(parent, false);
        if (instance.Parent != null)
            ResortSiblings(instance.Parent);
    }

    private static Transform FindUIParent(LuaInstance instance)
    {
        var p = instance.Parent;
        while (p != null)
        {
            if (p.UnityObject != null && p.UnityObject.GetComponent<RectTransform>() != null)
                return p.UnityObject.transform;
            p = p.Parent;
        }
        return EnsureRootCanvas().transform;
    }

    private static Canvas rootCanvas;

    public static Canvas EnsureRootCanvas()
    {
        if (rootCanvas != null) return rootCanvas;
        rootCanvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
        if (rootCanvas != null) return rootCanvas;

        var go = new GameObject("GuiRoot");
        rootCanvas = go.AddComponent<Canvas>();
        rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();
        return rootCanvas;
    }

    protected static void ApplyZIndex(LuaInstance instance)
    {
        if (instance.Parent != null) ResortSiblings(instance.Parent);
    }

    // UGUI draws by sibling order, so to honor ZIndex we sort the parent's
    // GUI children by ZIndex (stable for ties) and replay SetSiblingIndex.
    private static readonly List<LuaInstance> sortBuffer = new();
    private static void ResortSiblings(LuaInstance parent)
    {
        sortBuffer.Clear();
        for (int i = 0; i < parent.Children.Count; i++)
        {
            var c = parent.Children[i];
            if (c.ClassDef is GUIBase && c.UnityObject != null)
                sortBuffer.Add(c);
        }
        sortBuffer.Sort((a, b) =>
        {
            int za = ((GUIBase)a.ClassDef).GetZIndex(a);
            int zb = ((GUIBase)b.ClassDef).GetZIndex(b);
            return za.CompareTo(zb);
        });
        for (int i = 0; i < sortBuffer.Count; i++)
            sortBuffer[i].UnityObject.transform.SetSiblingIndex(i);
        sortBuffer.Clear();
    }

    // Subclasses call this after creating their UnityObject so we can do the
    // initial parenting + visibility hookup uniformly.
    protected static void OnUnityObjectCreated(LuaInstance instance)
    {
        Reparent(instance);
        ApplyEffectiveVisible(instance);
    }
}

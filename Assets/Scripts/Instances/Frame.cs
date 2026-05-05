using MoonSharp.Interpreter;
using UnityEngine;
using UnityEngine.UI;

public class Frame : ShadableUI
{
    public override string ClassName => "Frame";

    public override bool Clonable => true;

    private class State
    {
        public LuaUDim2 Size = GUIRect.DefaultSize;
        public LuaUDim2 Position = GUIRect.DefaultPosition;
        public LuaColor3 BackgroundColor = new LuaColor3(1f, 1f, 1f);
        public float BackgroundTransparency = 0f;
        public bool ClipDescendants = false;
        public Image Image;
        public RectMask2D Mask;
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
            d.BackgroundColor = s.BackgroundColor;
            d.BackgroundTransparency = s.BackgroundTransparency;
            d.ClipDescendants = s.ClipDescendants;
        }
    }

    public override void OnEnterScene(LuaInstance instance)
    {
        if (instance.UnityObject != null) return;
        var s = (State)instance.UserState;

        var go = new GameObject(instance.Name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        s.Image = go.AddComponent<Image>();
        instance.UnityObject = go;

        OnUnityObjectCreated(instance);
        ApplyBackground(s);
        ApplyClip(instance, s);
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
                $"Frame import: GameObject \"{go.name}\" has no RectTransform — only UI objects can be wrapped as Frame");

        // Reuse an existing Image so editor-tweaked sprites/colors survive,
        // or add one if the object is a bare RectTransform.
        s.Image = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        s.Mask = go.GetComponent<RectMask2D>();
        s.ClipDescendants = s.Mask != null && s.Mask.enabled;

        // The current rect is round-tripped as a pure-offset UDim2 — Scale
        // would require knowing the parent at import time, which we don't.
        var size = rt.rect.size;
        s.Size = new LuaUDim2(new LuaUDim(0f, size.x), new LuaUDim(0f, size.y));
        var ap = rt.anchoredPosition;
        s.Position = new LuaUDim2(new LuaUDim(0f, ap.x), new LuaUDim(0f, -ap.y));

        var c = s.Image.color;
        s.BackgroundColor = new LuaColor3(c.r, c.g, c.b);
        s.BackgroundTransparency = 1f - c.a;

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

    public override void OnChildAdded(LuaInstance instance, LuaInstance child)
    {
        // Re-apply size on children when this frame's size depends on parent —
        // and re-apply ours so any pending child rect uses the latest layout.
        var s = (State)instance.UserState;
        if (instance.UnityObject != null)
            GUIRect.Apply((RectTransform)instance.UnityObject.transform, s.Size, s.Position, GetAnchorPoint(instance));
    }

    public override bool TryGetProperty(LuaInstance instance, string key, out DynValue value)
    {
        var s = (State)instance.UserState;
        switch (key)
        {
            case "Size": value = UserData.Create(s.Size); return true;
            case "Position": value = UserData.Create(s.Position); return true;
            case "BackgroundColor":
            case "BackgroundColor3":
                value = UserData.Create(s.BackgroundColor); return true;
            case "BackgroundTransparency":
                value = DynValue.NewNumber(s.BackgroundTransparency); return true;
            case "ClipDescendants":
                value = DynValue.NewBoolean(s.ClipDescendants); return true;
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
            case "BackgroundColor":
            case "BackgroundColor3":
                s.BackgroundColor = GUIRect.ResolveColor3(value, "BackgroundColor");
                ApplyBackground(s);
                return true;
            case "BackgroundTransparency":
                if (value.Type != DataType.Number)
                    throw new ScriptRuntimeException("BackgroundTransparency must be a number");
                s.BackgroundTransparency = Mathf.Clamp01((float)value.Number);
                ApplyBackground(s);
                return true;
            case "ClipDescendants":
                if (value.Type != DataType.Boolean)
                    throw new ScriptRuntimeException("ClipDescendants must be a boolean");
                s.ClipDescendants = value.Boolean;
                ApplyClip(instance, s);
                return true;
        }
        return base.TrySetProperty(instance, key, value);
    }

    private static void ApplyBackground(State s)
    {
        if (s.Image == null) return;
        s.Image.color = new Color(
            s.BackgroundColor.R,
            s.BackgroundColor.G,
            s.BackgroundColor.B,
            1f - s.BackgroundTransparency);
    }

    private static void ApplyClip(LuaInstance instance, State s)
    {
        if (instance.UnityObject == null) return;
        if (s.ClipDescendants)
        {
            if (s.Mask == null)
                s.Mask = instance.UnityObject.AddComponent<RectMask2D>();
            s.Mask.enabled = true;
        }
        else if (s.Mask != null)
        {
            s.Mask.enabled = false;
        }
    }
}

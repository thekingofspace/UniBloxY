using MoonSharp.Interpreter;
using UnityEngine;
using UnityEngine.UI;

public class BillboardGui : GUIBase
{
    public override string ClassName => "BillboardGui";
    public override bool Clonable => true;

    private class State
    {
        public bool Enabled = true;
        public LuaUDim2 Size = new LuaUDim2(new LuaUDim(0f, 200f), new LuaUDim(0f, 50f));
        public bool AlwaysOnTop = false;
        public LuaVector3 Offset = LuaVector3.Zero;
        public Canvas Canvas;
        public BillboardFollower Follower;
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
            d.Enabled = s.Enabled;
            d.Size = s.Size;
            d.AlwaysOnTop = s.AlwaysOnTop;
            d.Offset = s.Offset;
        }
    }

    public override void OnEnterScene(LuaInstance instance)
    {
        if (instance.UnityObject != null) return;
        var s = (State)instance.UserState;

        // Parent under the shared overlay root as a sub-canvas. A standalone
        // ScreenSpaceOverlay canvas has its RectTransform pose driven by Unity
        // each frame (forced to fill the screen), which would overwrite the
        // follower's per-frame position write — so the billboard would never
        // appear where its 3D source is.
        var root = GUIBase.EnsureRootCanvas();

        var go = new GameObject(instance.Name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(root.transform, false);
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot     = new Vector2(0.5f, 0.5f);

        s.Canvas = go.AddComponent<Canvas>();
        s.Canvas.overrideSorting = true;
        go.AddComponent<GraphicRaycaster>();

        s.Follower = go.AddComponent<BillboardFollower>();
        s.Follower.Canvas = s.Canvas;
        s.Follower.Rect = rt;

        instance.UnityObject = go;
        ApplySize(instance, s);
        ApplyAlwaysOnTop(s);
        UpdateFollowerSource(instance, s);
        ApplyEnabled(instance, s);
    }

    public override void OnExitScene(LuaInstance instance)
    {
        if (instance.UnityObject != null)
        {
            Object.Destroy(instance.UnityObject);
            instance.UnityObject = null;
        }
    }

    public override void OnAncestryChanged(LuaInstance instance)
    {

        var s = (State)instance.UserState;
        UpdateFollowerSource(instance, s);
        ApplyEffectiveVisible(instance);
        ApplyEnabled(instance, s);
    }

    private static void UpdateFollowerSource(LuaInstance instance, State s)
    {
        if (s.Follower == null) return;
        var parentGo = instance.Parent?.UnityObject;

        if (parentGo != null && parentGo.GetComponent<RectTransform>() == null)
        {
            s.Follower.Source = parentGo.transform;
        }
        else
        {
            s.Follower.Source = null;
        }
        s.Follower.Offset = new Vector3(s.Offset.X, s.Offset.Y, s.Offset.Z);
    }

    private static void ApplySize(LuaInstance instance, State s)
    {
        if (instance.UnityObject == null) return;
        var rt = (RectTransform)instance.UnityObject.transform;

        var screen = new Vector2(Screen.width, Screen.height);
        rt.sizeDelta = new Vector2(
            s.Size.X.Offset + s.Size.X.Scale * screen.x,
            s.Size.Y.Offset + s.Size.Y.Scale * screen.y);
    }

    private static void ApplyAlwaysOnTop(State s)
    {
        if (s.Canvas == null) return;
        s.Canvas.sortingOrder = s.AlwaysOnTop ? 32760 : 0;
    }

    private static void ApplyEnabled(LuaInstance instance, State s)
    {
        if (s.Follower != null) s.Follower.Enabled = s.Enabled && EffectiveVisible(instance);

        if (s.Canvas != null && (!s.Enabled || s.Follower == null || s.Follower.Source == null))
            s.Canvas.enabled = false;
    }

    public override bool TryGetProperty(LuaInstance instance, string key, out DynValue value)
    {
        var s = (State)instance.UserState;
        switch (key)
        {
            case "Enabled":     value = DynValue.NewBoolean(s.Enabled);     return true;
            case "Size":        value = UserData.Create(s.Size);            return true;
            case "AlwaysOnTop": value = DynValue.NewBoolean(s.AlwaysOnTop); return true;
            case "Offset":      value = UserData.Create(s.Offset);          return true;
        }
        return base.TryGetProperty(instance, key, out value);
    }

    public override bool TrySetProperty(LuaInstance instance, string key, DynValue value)
    {
        var s = (State)instance.UserState;
        switch (key)
        {
            case "Enabled":
                if (value.Type != DataType.Boolean)
                    throw new ScriptRuntimeException("BillboardGui.Enabled must be a boolean");
                s.Enabled = value.Boolean;
                base.TrySetProperty(instance, "Visible", DynValue.NewBoolean(value.Boolean));
                ApplyEnabled(instance, s);
                return true;
            case "Size":
                s.Size = GUIRect.ResolveUDim2(value, "Size");
                ApplySize(instance, s);
                return true;
            case "AlwaysOnTop":
                if (value.Type != DataType.Boolean)
                    throw new ScriptRuntimeException("BillboardGui.AlwaysOnTop must be a boolean");
                s.AlwaysOnTop = value.Boolean;
                ApplyAlwaysOnTop(s);
                return true;
            case "Offset":
                if (value.Type == DataType.UserData && value.UserData.Object is LuaVector3 v)
                {
                    s.Offset = v;
                    if (s.Follower != null) s.Follower.Offset = new Vector3(v.X, v.Y, v.Z);
                    return true;
                }
                throw new ScriptRuntimeException("BillboardGui.Offset must be a Vector3");
        }
        return base.TrySetProperty(instance, key, value);
    }
}

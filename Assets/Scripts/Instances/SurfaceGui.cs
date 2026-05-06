using MoonSharp.Interpreter;
using UnityEngine;
using UnityEngine.UI;

public class SurfaceGui : GUIBase
{
    public override string ClassName => "SurfaceGui";
    public override bool Clonable => true;

    private class State
    {
        public bool Wrap = true;
        public float Angle = 0f;
        public bool Enabled = true;
        public Canvas Canvas;
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
            d.Wrap = s.Wrap;
            d.Angle = s.Angle;
            d.Enabled = s.Enabled;
        }
    }

    public override void OnEnterScene(LuaInstance instance)
    {
        if (instance.UnityObject != null) return;
        var s = (State)instance.UserState;

        var go = new GameObject(instance.Name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(100f, 100f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);

        s.Canvas = go.AddComponent<Canvas>();
        s.Canvas.renderMode = RenderMode.WorldSpace;
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();

        instance.UnityObject = go;
        AttachToParent(instance);
        ApplyWrapAndAngle(instance, s);
        ApplyEffectiveVisible(instance);
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

        AttachToParent(instance);
        ApplyEffectiveVisible(instance);
        ApplyWrapAndAngle(instance, (State)instance.UserState);
    }

    private static void AttachToParent(LuaInstance instance)
    {
        var go = instance.UnityObject;
        if (go == null) return;
        var parentGo = instance.Parent?.UnityObject;
        if (parentGo != null && parentGo.GetComponent<RectTransform>() == null)
        {

            go.transform.SetParent(parentGo.transform, false);
        }
        else
        {
            go.transform.SetParent(null, true);
        }
    }

    private static void ApplyWrapAndAngle(LuaInstance instance, State s)
    {
        var go = instance.UnityObject;
        if (go == null) return;
        var rt = (RectTransform)go.transform;
        var parentGo = instance.Parent?.UnityObject;

        if (parentGo != null && parentGo.GetComponent<RectTransform>() == null)
        {

            Vector3 size;
            var renderer = parentGo.GetComponent<Renderer>();
            if (renderer != null)
            {

                var mf = parentGo.GetComponent<MeshFilter>();
                size = mf != null && mf.sharedMesh != null ? mf.sharedMesh.bounds.size : Vector3.one;
            }
            else
            {
                size = Vector3.one;
            }

            if (s.Wrap)
            {
                rt.localScale = new Vector3(size.x / rt.sizeDelta.x, size.y / rt.sizeDelta.y, 1f);
            }
            else
            {

                rt.localScale = new Vector3(1f / rt.sizeDelta.x, 1f / rt.sizeDelta.y, 1f);
            }

            var halfDepth = size.z * 0.5f;
            rt.localPosition = new Vector3(0f, 0f, halfDepth + 0.001f);
            rt.localRotation = Quaternion.AngleAxis(s.Angle, Vector3.forward);
        }
    }

    public override bool TryGetProperty(LuaInstance instance, string key, out DynValue value)
    {
        var s = (State)instance.UserState;
        switch (key)
        {
            case "Wrap":    value = DynValue.NewBoolean(s.Wrap);    return true;
            case "Angle":   value = DynValue.NewNumber(s.Angle);    return true;
            case "Enabled": value = DynValue.NewBoolean(s.Enabled); return true;
        }
        return base.TryGetProperty(instance, key, out value);
    }

    public override bool TrySetProperty(LuaInstance instance, string key, DynValue value)
    {
        var s = (State)instance.UserState;
        switch (key)
        {
            case "Wrap":
                if (value.Type != DataType.Boolean)
                    throw new ScriptRuntimeException("SurfaceGui.Wrap must be a boolean");
                s.Wrap = value.Boolean;
                ApplyWrapAndAngle(instance, s);
                return true;
            case "Angle":
                if (value.Type != DataType.Number)
                    throw new ScriptRuntimeException("SurfaceGui.Angle must be a number");
                s.Angle = Mathf.Clamp((float)value.Number, -180f, 180f);
                ApplyWrapAndAngle(instance, s);
                return true;
            case "Enabled":
                if (value.Type != DataType.Boolean)
                    throw new ScriptRuntimeException("SurfaceGui.Enabled must be a boolean");
                s.Enabled = value.Boolean;

                base.TrySetProperty(instance, "Visible", DynValue.NewBoolean(value.Boolean));
                return true;
        }
        return base.TrySetProperty(instance, key, value);
    }
}

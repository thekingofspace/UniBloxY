using System.Collections.Generic;
using MoonSharp.Interpreter;
using UnityEngine;
using UnityEngine.InputSystem;

public enum ListenerKind { Mouse, Instance }

[MoonSharpUserData]
public class LuaListener
{
    public string ClassName => "Listener";
    public string Mode => kind == ListenerKind.Mouse ? "Mouse" : "Instance";
    public bool Destroyed { get; private set; }

    [MoonSharpHidden] public ListenerKind Kind => kind;
    [MoonSharpHidden] public LuaInstance Target => target;

    private readonly Script script;
    private readonly ListenerKind kind;
    private readonly LuaInstance target;

    private readonly List<LuaInstance> trackers = new();
    private readonly HashSet<LuaInstance> hovered = new();
    private readonly HashSet<LuaInstance> pressed = new();

    private readonly Signal onEnterSig;
    private readonly Signal onLeaveSig;
    private readonly Signal onActivatedSig;
    private readonly Signal onReleaseSig;

    private readonly Table onEnterTbl;
    private readonly Table onLeaveTbl;
    private readonly Table onActivatedTbl;
    private readonly Table onReleaseTbl;

    public Table OnEnter => onEnterTbl;
    public Table OnLeave => onLeaveTbl;
    public Table OnActivated => onActivatedTbl;
    public Table OnRelease => onReleaseTbl;

    [MoonSharpHidden]
    public LuaListener(Script script, ListenerKind kind, LuaInstance target)
    {
        this.script = script;
        this.kind = kind;
        this.target = target;

        onEnterSig     = new Signal(script, "Listener.OnEnter");
        onLeaveSig     = new Signal(script, "Listener.OnLeave");
        onActivatedSig = new Signal(script, "Listener.OnActivated");
        onReleaseSig   = new Signal(script, "Listener.OnRelease");

        onEnterTbl     = onEnterSig.BuildTable();
        onLeaveTbl     = onLeaveSig.BuildTable();
        onActivatedTbl = onActivatedSig.BuildTable();
        onReleaseTbl   = onReleaseSig.BuildTable();
    }

    public void AddTracker(DynValue v)
    {
        if (Destroyed) return;
        var inst = LuaInstance.ResolveInstance(v);
        if (inst == null)
            throw new ScriptRuntimeException("Listener:AddTracker requires an Instance");
        if (!trackers.Contains(inst)) trackers.Add(inst);
    }

    public bool RemoveTracker(DynValue v)
    {
        if (Destroyed) return false;
        var inst = LuaInstance.ResolveInstance(v);
        if (inst == null) return false;

        if (pressed.Remove(inst)) onReleaseSig.Fire(inst.Table);
        if (hovered.Remove(inst)) onLeaveSig.Fire(inst.Table);
        return trackers.Remove(inst);
    }

    public DynValue GetTrackers()
    {
        var t = new Table(script);
        for (int i = 0; i < trackers.Count; i++)
            t[i + 1] = DynValue.NewTable(trackers[i].Table);
        return DynValue.NewTable(t);
    }

    public void Destroy()
    {
        if (Destroyed) return;

        foreach (var inst in pressed)
            if (inst != null) onReleaseSig.Fire(inst.Table);
        foreach (var inst in hovered)
            if (inst != null && !pressed.Contains(inst)) onLeaveSig.Fire(inst.Table);
        pressed.Clear();
        hovered.Clear();
        trackers.Clear();
        Destroyed = true;
    }

    [MoonSharpHidden]
    public void Tick()
    {
        if (Destroyed) return;
        if (kind == ListenerKind.Mouse) TickMouse();
        else TickInstance();
    }

    private static bool IsLive(LuaInstance inst)
    {
        if (inst == null) return false;
        var go = inst.UnityObject;
        if (go == null) return false;
        if (!go.activeInHierarchy) return false;
        if (!GUIBase.EffectiveVisible(inst)) return false;
        if (!Renderable.EffectiveRender(inst)) return false;
        return true;
    }

    private void ForceLeave(LuaInstance t)
    {
        if (pressed.Remove(t)) onReleaseSig.Fire(t.Table);
        if (hovered.Remove(t)) onLeaveSig.Fire(t.Table);
    }

    private void DrainAll()
    {
        if (pressed.Count > 0)
        {
            var snap = new LuaInstance[pressed.Count];
            pressed.CopyTo(snap);
            pressed.Clear();
            for (int i = 0; i < snap.Length; i++)
                if (snap[i] != null) onReleaseSig.Fire(snap[i].Table);
        }
        if (hovered.Count > 0)
        {
            var snap = new LuaInstance[hovered.Count];
            hovered.CopyTo(snap);
            hovered.Clear();
            for (int i = 0; i < snap.Length; i++)
                if (snap[i] != null) onLeaveSig.Fire(snap[i].Table);
        }
    }

    private void TickMouse()
    {
        var m = Mouse.current;
        if (m == null) return;
        var screenPos = m.position.ReadValue();
        bool leftDown = m.leftButton.isPressed;

        for (int i = trackers.Count - 1; i >= 0; i--)
        {
            var t = trackers[i];
            if (t == null) { trackers.RemoveAt(i); continue; }

            if (!IsLive(t))
            {
                ForceLeave(t);
                continue;
            }

            bool hit = MouseHits(t, screenPos);
            bool wasHovering = hovered.Contains(t);
            bool wasPressed = pressed.Contains(t);

            if (hit && !wasHovering)
            {
                hovered.Add(t);
                onEnterSig.Fire(t.Table);
            }
            else if (!hit && wasHovering)
            {
                ForceLeave(t);
                continue;
            }

            if (hovered.Contains(t))
            {
                if (leftDown && !wasPressed)
                {
                    pressed.Add(t);
                    onActivatedSig.Fire(t.Table);
                }
                else if (!leftDown && wasPressed)
                {
                    pressed.Remove(t);
                    onReleaseSig.Fire(t.Table);
                }
            }
        }
    }

    private static bool MouseHits(LuaInstance t, Vector2 screenPos)
    {
        var go = t.UnityObject;
        if (go == null) return false;

        if (go.TryGetComponent<RectTransform>(out var rt))
        {

            var canvas = rt.GetComponentInParent<Canvas>();
            UnityEngine.Camera uiCam = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                uiCam = canvas.worldCamera != null ? canvas.worldCamera : UnityEngine.Camera.main;
            return RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos, uiCam);
        }

        var cam = UnityEngine.Camera.main;
        if (cam == null) return false;
        var ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out var hitInfo, 1000f))
        {
            var hitGo = hitInfo.collider.gameObject;
            if (hitGo == go) return true;
            if (hitGo.transform.IsChildOf(go.transform)) return true;
        }
        return false;
    }

    private void TickInstance()
    {
        if (target == null) return;

        if (!IsLive(target))
        {
            DrainAll();
            return;
        }

        bool targetIs2D = target.UnityObject.TryGetComponent<RectTransform>(out _);

        for (int i = trackers.Count - 1; i >= 0; i--)
        {
            var t = trackers[i];
            if (t == null) { trackers.RemoveAt(i); continue; }

            if (!IsLive(t))
            {
                ForceLeave(t);
                continue;
            }

            bool trackerIs2D = t.UnityObject.TryGetComponent<RectTransform>(out _);

            if (trackerIs2D != targetIs2D)
            {
                ForceLeave(t);
                continue;
            }

            bool overlap = targetIs2D ? OverlapRect(t, target) : OverlapBounds(t, target);
            bool wasHovering = hovered.Contains(t);

            if (overlap && !wasHovering)
            {
                hovered.Add(t);
                onEnterSig.Fire(t.Table);
            }
            else if (!overlap && wasHovering)
            {
                hovered.Remove(t);
                onLeaveSig.Fire(t.Table);
            }
        }
    }

    private static bool OverlapBounds(LuaInstance a, LuaInstance b)
    {

        if (!a.UnityObject.TryGetComponent<Renderer>(out var rA))
            rA = a.UnityObject.GetComponentInChildren<Renderer>();
        if (!b.UnityObject.TryGetComponent<Renderer>(out var rB))
            rB = b.UnityObject.GetComponentInChildren<Renderer>();
        if (rA == null || rB == null) return false;
        return rA.bounds.Intersects(rB.bounds);
    }

    private static readonly Vector3[] cornerScratchA = new Vector3[4];
    private static readonly Vector3[] cornerScratchB = new Vector3[4];

    private static bool OverlapRect(LuaInstance a, LuaInstance b)
    {
        if (!a.UnityObject.TryGetComponent<RectTransform>(out var rtA)) return false;
        if (!b.UnityObject.TryGetComponent<RectTransform>(out var rtB)) return false;

        rtA.GetWorldCorners(cornerScratchA);
        rtB.GetWorldCorners(cornerScratchB);

        var rA = new Rect(cornerScratchA[0].x, cornerScratchA[0].y,
            cornerScratchA[2].x - cornerScratchA[0].x,
            cornerScratchA[2].y - cornerScratchA[0].y);
        var rB = new Rect(cornerScratchB[0].x, cornerScratchB[0].y,
            cornerScratchB[2].x - cornerScratchB[0].x,
            cornerScratchB[2].y - cornerScratchB[0].y);
        return rA.Overlaps(rB);
    }

    public override string ToString() =>
        $"Listener<{Mode}{(target != null ? ":" + target.Name : "")}>";
}

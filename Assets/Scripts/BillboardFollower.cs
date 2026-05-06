using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class BillboardFollower : MonoBehaviour
{
    public Transform Source;
    public Vector3 Offset;
    public bool Enabled = true;
    public Canvas Canvas;
    public RectTransform Rect;

    // BillboardGui bakes Size.Scale*ReferencePPS + Size.Offset into the rect's
    // sizeDelta as a constant baseline. Each frame we set localScale to
    // pixelsPerStud(distance) / ReferencePPS so the rendered size tracks the
    // world-space size — closer to camera = bigger, farther = smaller, like
    // Roblox. localScale propagates through the transform hierarchy so child
    // Frames scale automatically without per-frame layout work.
    public float ReferencePPS = 100f;

    // When false, the billboard hides if any 3D MeshRenderer's bounds blocks
    // the line from camera to source — i.e., it respects 3D occlusion. When
    // true, the billboard renders regardless of what's in front of it.
    // Either way the canvas's sortingOrder keeps it behind regular GUI; this
    // flag only governs visibility against the 3D world.
    public bool AlwaysOnTop = false;

    // Camera.main does an internal tag scan when its cache is invalidated, and
    // every BillboardFollower hits it every LateUpdate. Cache the resolved
    // camera and only re-resolve when the prior reference is destroyed.
    private static UnityEngine.Camera cachedCam;

    // The project has no colliders on parts, so Physics.Linecast can't be used
    // for occlusion. Instead we test the camera->source ray against every
    // active MeshRenderer's world-space bounds. Build the candidate list once
    // per frame and share it across every follower so this stays O(parts) per
    // frame, not O(parts × billboards).
    private static readonly List<MeshRenderer> occluderBuf = new List<MeshRenderer>(64);
    private static int occluderBufFrame = -1;

    private static void RebuildOccluderListIfStale()
    {
        int frame = Time.frameCount;
        if (occluderBufFrame == frame) return;
        occluderBufFrame = frame;
        occluderBuf.Clear();
        var all = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            var r = all[i];
            if (r != null && r.enabled && r.gameObject.activeInHierarchy)
                occluderBuf.Add(r);
        }
    }

    private bool IsOccluded(Vector3 from, Vector3 to)
    {
        var dir = to - from;
        float dist = dir.magnitude;
        if (dist < 0.001f) return false;
        var ray = new Ray(from, dir / dist);

        RebuildOccluderListIfStale();
        var sourceT = Source;
        for (int i = 0; i < occluderBuf.Count; i++)
        {
            var r = occluderBuf[i];
            if (r == null) continue;
            // Don't let the source (or anything below it) occlude itself —
            // the GUI sits above its part, and a near-grazing ray angle
            // would otherwise clip through the part itself.
            if (sourceT != null && r.transform.IsChildOf(sourceT)) continue;
            if (r.bounds.IntersectRay(ray, out float hitDist) && hitDist > 0.001f && hitDist < dist)
                return true;
        }
        return false;
    }

    void LateUpdate()
    {
        if (Canvas == null) Canvas = GetComponent<Canvas>();
        if (Rect == null) Rect = (RectTransform)transform;

        if (!Enabled || Source == null)
        {
            if (Canvas != null) Canvas.enabled = false;
            return;
        }

        if (cachedCam == null) cachedCam = UnityEngine.Camera.main;
        var cam = cachedCam;
        if (cam == null)
        {
            if (Canvas != null) Canvas.enabled = false;
            return;
        }

        var worldPos = Source.position + Offset;
        var screen = cam.WorldToScreenPoint(worldPos);

        if (screen.z < 0f)
        {
            if (Canvas != null) Canvas.enabled = false;
            return;
        }

        if (!AlwaysOnTop && IsOccluded(cam.transform.position, worldPos))
        {
            if (Canvas != null) Canvas.enabled = false;
            return;
        }

        if (Canvas != null) Canvas.enabled = true;

        // 1 stud at distance d projects to Screen.height / (2*d*tan(fov_y/2))
        // vertical pixels under perspective. For ortho cameras the conversion
        // is constant.
        float pixelsPerStud;
        if (cam.orthographic)
        {
            pixelsPerStud = Screen.height / (2f * cam.orthographicSize);
        }
        else
        {
            float distance = Mathf.Max(screen.z, 0.01f);
            pixelsPerStud = Screen.height /
                (2f * distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad));
        }
        float s = pixelsPerStud / ReferencePPS;
        transform.localScale = new Vector3(s, s, 1f);

        // Rect is anchored to the bottom-left of the parent overlay canvas
        // (anchorMin = anchorMax = (0,0)) and pivoted at its center, so an
        // anchoredPosition equal to the screen point places the rect's center
        // exactly at the projected world point.
        Rect.anchoredPosition = new Vector2(screen.x, screen.y);
    }
}

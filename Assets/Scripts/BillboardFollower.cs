using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class BillboardFollower : MonoBehaviour
{
    public Transform Source;
    public Vector3 Offset;
    public bool Enabled = true;
    public Canvas Canvas;
    public RectTransform Rect;

    void LateUpdate()
    {
        if (Canvas == null) Canvas = GetComponent<Canvas>();
        if (Rect == null) Rect = (RectTransform)transform;

        if (!Enabled || Source == null)
        {
            if (Canvas != null) Canvas.enabled = false;
            return;
        }

        var cam = UnityEngine.Camera.main;
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

        if (Canvas != null) Canvas.enabled = true;

        // Rect is anchored to the bottom-left of the parent overlay canvas
        // (anchorMin = anchorMax = (0,0)) and pivoted at its center, so an
        // anchoredPosition equal to the screen point places the rect's center
        // exactly at the projected world point.
        Rect.anchoredPosition = new Vector2(screen.x, screen.y);
    }
}

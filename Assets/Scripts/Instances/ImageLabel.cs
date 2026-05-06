using MoonSharp.Interpreter;
using UnityEngine;
using UnityEngine.UI;

public class ImageLabel : ShadableUI
{
    public override string ClassName => "ImageLabel";

    public override bool Clonable => true;

    private class State
    {
        public LuaUDim2 Size = GUIRect.DefaultSize;
        public LuaUDim2 Position = GUIRect.DefaultPosition;
        public LuaImage Image;
        public LuaColor3 ImageColor = new LuaColor3(1f, 1f, 1f);
        public float ImageTransparency = 0f;
        public Image ImageComp;
        public string ScaleType = "Stretch";

        public Sprite CroppedSprite;
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
            d.Image = s.Image;
            d.ImageColor = s.ImageColor;
            d.ImageTransparency = s.ImageTransparency;
            d.ScaleType = s.ScaleType;

        }
    }

    public override void OnEnterScene(LuaInstance instance)
    {
        if (instance.UnityObject != null) return;
        var s = (State)instance.UserState;

        var go = new GameObject(instance.Name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        s.ImageComp = go.AddComponent<Image>();
        instance.UnityObject = go;

        OnUnityObjectCreated(instance);

        GUIRect.Apply(rt, s.Size, s.Position, GetAnchorPoint(instance));
        ApplyImage(instance, s);
        ApplyMaterial(instance);
    }

    public override void OnExitScene(LuaInstance instance)
    {
        if (instance.UserState is State s) DropCroppedSprite(s);
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
                $"ImageLabel import: GameObject \"{go.name}\" has no RectTransform — only UI objects can be wrapped as ImageLabel");

        s.ImageComp = go.GetComponent<Image>() ?? go.AddComponent<Image>();

        var size = rt.rect.size;
        s.Size = new LuaUDim2(new LuaUDim(0f, size.x), new LuaUDim(0f, size.y));
        var ap = rt.anchoredPosition;
        s.Position = new LuaUDim2(new LuaUDim(0f, ap.x), new LuaUDim(0f, -ap.y));

        var sprite = s.ImageComp.sprite;
        if (sprite != null)
            s.Image = new LuaImage(sprite.name ?? "", sprite);

        var c = s.ImageComp.color;
        s.ImageColor = new LuaColor3(c.r, c.g, c.b);
        s.ImageTransparency = 1f - c.a;

        if (s.ImageComp.type == Image.Type.Tiled) s.ScaleType = "Tile";
        else if (s.ImageComp.preserveAspect)      s.ScaleType = "Fit";
        else                                      s.ScaleType = "Stretch";

        StoreAnchorPoint(instance, new LuaVector2(rt.pivot.x, 1f - rt.pivot.y));
    }

    protected override void ReapplyRect(LuaInstance instance)
    {
        if (instance.UnityObject == null) return;
        var s = (State)instance.UserState;
        GUIRect.Apply((RectTransform)instance.UnityObject.transform, s.Size, s.Position, GetAnchorPoint(instance));

        if (s.ScaleType == "Crop") ApplyImage(instance, s);
    }

    protected override void SetPosition(LuaInstance instance, LuaUDim2 position)
    {
        ((State)instance.UserState).Position = position;
    }

    public override bool TryGetProperty(LuaInstance instance, string key, out DynValue value)
    {
        var s = (State)instance.UserState;
        switch (key)
        {
            case "Size": value = UserData.Create(s.Size); return true;
            case "Position": value = UserData.Create(s.Position); return true;
            case "Image": value = s.Image != null ? UserData.Create(s.Image) : DynValue.Nil; return true;
            case "ImageColor":
            case "ImageColor3":
                value = UserData.Create(s.ImageColor); return true;
            case "ImageTransparency": value = DynValue.NewNumber(s.ImageTransparency); return true;
            case "ScaleType": value = DynValue.NewString(s.ScaleType); return true;
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
            case "Image":
                if (value.IsNil()) { s.Image = null; ApplyImage(instance, s); return true; }
                if (value.Type == DataType.UserData && value.UserData.Object is LuaImage img)
                {
                    s.Image = img;
                    ApplyImage(instance, s);
                    return true;
                }
                throw new ScriptRuntimeException("Image must be an Image from AssetService:GetImage");
            case "ImageColor":
            case "ImageColor3":
                s.ImageColor = GUIRect.ResolveColor3(value, "ImageColor");
                ApplyImage(instance, s);
                return true;
            case "ImageTransparency":
                if (value.Type != DataType.Number)
                    throw new ScriptRuntimeException("ImageTransparency must be a number");
                s.ImageTransparency = Mathf.Clamp01((float)value.Number);
                ApplyImage(instance, s);
                return true;
            case "ScaleType":
                if (value.Type != DataType.String)
                    throw new ScriptRuntimeException("ScaleType must be a string");
                var resolved = NormalizeScaleType(value.String);
                if (resolved == null)
                    throw new ScriptRuntimeException(
                        $"ScaleType \"{value.String}\" is not recognized (Stretch/Fit/Tile/Crop)");
                s.ScaleType = resolved;
                ApplyImage(instance, s);
                return true;
        }
        return base.TrySetProperty(instance, key, value);
    }

    private static string NormalizeScaleType(string value)
    {
        if (string.IsNullOrEmpty(value)) return null;
        switch (value.Trim().ToLowerInvariant())
        {
            case "stretch": return "Stretch";
            case "fit":
            case "contain": return "Fit";
            case "tile":
            case "repeat":  return "Tile";
            case "crop":
            case "cover":   return "Crop";
        }
        return null;
    }

    private static void ApplyImage(LuaInstance instance, State s)
    {
        if (s.ImageComp == null) return;

        Sprite display;
        switch (s.ScaleType)
        {
            case "Crop":
                display = ComputeCroppedSprite(instance, s);
                s.ImageComp.preserveAspect = false;
                s.ImageComp.type = Image.Type.Simple;
                break;
            case "Tile":
                DropCroppedSprite(s);
                display = s.Image?.Sprite;
                s.ImageComp.preserveAspect = false;
                s.ImageComp.type = Image.Type.Tiled;
                break;
            case "Fit":
                DropCroppedSprite(s);
                display = s.Image?.Sprite;
                s.ImageComp.preserveAspect = true;
                s.ImageComp.type = Image.Type.Simple;
                break;
            case "Stretch":
            default:
                DropCroppedSprite(s);
                display = s.Image?.Sprite;
                s.ImageComp.preserveAspect = false;
                s.ImageComp.type = Image.Type.Simple;
                break;
        }

        s.ImageComp.sprite = display;

        var alpha = (s.Image != null && display != null) ? (1f - s.ImageTransparency) : 0f;
        s.ImageComp.color = new Color(
            s.ImageColor.R, s.ImageColor.G, s.ImageColor.B, alpha);
    }

    private static Sprite ComputeCroppedSprite(LuaInstance instance, State s)
    {
        var src = s.Image?.Sprite;
        var tex = s.Image?.Texture;
        if (src == null || tex == null)
        {
            DropCroppedSprite(s);
            return null;
        }

        var rt = instance.UnityObject != null
            ? instance.UnityObject.transform as RectTransform
            : null;
        var rectSize = rt != null ? rt.rect.size : Vector2.zero;
        if (rectSize.x <= 0f || rectSize.y <= 0f)
        {

            DropCroppedSprite(s);
            return src;
        }

        var srcRect = src.rect;
        var imgAspect = srcRect.width / srcRect.height;
        var rectAspect = rectSize.x / rectSize.y;

        Rect cropRect;
        if (imgAspect > rectAspect)
        {

            var newWidth = srcRect.height * rectAspect;
            var x = srcRect.x + (srcRect.width - newWidth) * 0.5f;
            cropRect = new Rect(x, srcRect.y, newWidth, srcRect.height);
        }
        else
        {

            var newHeight = srcRect.width / rectAspect;
            var y = srcRect.y + (srcRect.height - newHeight) * 0.5f;
            cropRect = new Rect(srcRect.x, y, srcRect.width, newHeight);
        }

        DropCroppedSprite(s);
        var pivot = new Vector2(0.5f, 0.5f);
        var ppu = src.pixelsPerUnit > 0f ? src.pixelsPerUnit : 100f;
        s.CroppedSprite = Sprite.Create(tex, cropRect, pivot, ppu);
        s.CroppedSprite.name = (src.name ?? "Image") + "_Cropped";
        return s.CroppedSprite;
    }

    private static void DropCroppedSprite(State s)
    {
        if (s.CroppedSprite != null)
        {
            Object.Destroy(s.CroppedSprite);
            s.CroppedSprite = null;
        }
    }
}

using System.Collections.Generic;
using MoonSharp.Interpreter;
using UnityEngine;
using UnityEngine.Rendering;
#if URP_VOLUME
using UnityEngine.Rendering.Universal;
#endif

[MoonSharpUserData]
public class LuaRaycastParams
{
    public string ClassName => "RaycastParams";
    public LuaVector3 Start { get; set; }
    public LuaVector3 Direction { get; set; }
    public float MaxDistance { get; set; } = 1000f;
    public DynValue Transformer { get; set; } = DynValue.Nil;
    public bool IgnoreUnityObjects { get; set; }
}

[MoonSharpUserData]
public class LuaRaycastResult
{
    public string ClassName => "RaycastResult";
    public LuaVector3 EndPosition { get; }
    public LuaVector3 Normal { get; }
    public float Distance { get; }
    public DynValue Object { get; }
    public string UnityObject { get; }

    public LuaRaycastResult(LuaVector3 endPosition, LuaVector3 normal, float distance,
                            DynValue obj, string unityObjectName)
    {
        EndPosition = endPosition;
        Normal = normal;
        Distance = distance;
        Object = obj ?? DynValue.Nil;
        UnityObject = unityObjectName;
    }
}

[MoonSharpUserData]
public class LuaPostProcessing
{
    public string ClassName => "PostProcessing";
    private readonly Dictionary<string, DynValue> overrides = new();

    [MoonSharpHidden]
    public void SetOverride(string key, DynValue val) => overrides[key] = val;

    public DynValue Get(string key) =>
        overrides.TryGetValue(key, out var v) ? v : DynValue.Nil;

    public void Set(string key, DynValue val)
    {
        if (val == null || val.IsNil()) overrides.Remove(key);
        else overrides[key] = val;
    }

    public bool Has(string key) => overrides.ContainsKey(key);
}

public class LightingService : LuaService
{
    private LuaPostProcessing post;

    private LuaColor3 ambient = new(0.5f, 0.5f, 0.5f);
    private LuaColor3 fogColor = new(0.7f, 0.8f, 0.9f);
    private float fogStart = 50f;
    private float fogEnd = 500f;
    private bool fogEnabled;
    private float exposure = 1f;
    private LuaTexture dirtTexture;
    private LuaTexture skybox;
    private float bloomThreshold = 1f;
    private float bloomIntensity;
    private float vignetteIntensity;
    private float saturation;
    private float contrast;

    public override void Register(Script script)
    {
        lua = script;

        UserData.RegisterType<LuaRaycastParams>();
        UserData.RegisterType<LuaRaycastResult>();
        UserData.RegisterType<LuaPostProcessing>();

        post = new LuaPostProcessing();

        script.Globals["RaycastParams"] = BuildRaycastParamsLib(script);
        script.Globals["Lighting"] = BuildLightingTable(script);
    }

    private static Table BuildRaycastParamsLib(Script script)
    {
        var t = new Table(script);
        t["new"] = DynValue.NewCallback((ctx, args) => UserData.Create(new LuaRaycastParams()));
        return t;
    }

    private Table BuildLightingTable(Script script)
    {
        var t = new Table(script);

        t["GetPostProcessing"] = DynValue.NewCallback((ctx, args) => UserData.Create(post));

        t["Raycast"] = DynValue.NewCallback((ctx, args) =>
        {

            int i = 0;
            if (args.Count > 0 && args[0].Type == DataType.Table) i = 1;
            var startVal = args.Count > i ? args[i] : DynValue.Nil;
            var dirVal = args.Count > i + 1 ? args[i + 1] : DynValue.Nil;
            var paramVal = args.Count > i + 2 ? args[i + 2] : DynValue.Nil;

            if (startVal.UserData?.Object is not LuaVector3 startV)
                throw new ScriptRuntimeException("Lighting:Raycast(start, direction): start must be a Vector3");
            if (dirVal.UserData?.Object is not LuaVector3 dirV)
                throw new ScriptRuntimeException("Lighting:Raycast(start, direction): direction must be a Vector3");

            LuaRaycastParams rp = paramVal.UserData?.Object as LuaRaycastParams;
            return DoRaycast(script, startV, dirV, rp);
        });

        var mt = new Table(script);
        mt["__type"] = "Lighting";
        mt["__index"] = (System.Func<DynValue, DynValue, DynValue>)((_, key) =>
        {
            if (key.Type != DataType.String) return DynValue.Nil;
            switch (key.String)
            {
                case "Ambient": return UserData.Create(ambient);
                case "FogColor": return UserData.Create(fogColor);
                case "FogStart": return DynValue.NewNumber(fogStart);
                case "FogEnd": return DynValue.NewNumber(fogEnd);
                case "FogEnabled": return DynValue.NewBoolean(fogEnabled);
                case "Exposure": return DynValue.NewNumber(exposure);
                case "DirtTexture": return dirtTexture != null ? UserData.Create(dirtTexture) : DynValue.Nil;
                case "Skybox": return skybox != null ? UserData.Create(skybox) : DynValue.Nil;
                case "BloomThreshold": return DynValue.NewNumber(bloomThreshold);
                case "BloomIntensity": return DynValue.NewNumber(bloomIntensity);
                case "VignetteIntensity": return DynValue.NewNumber(vignetteIntensity);
                case "Saturation": return DynValue.NewNumber(saturation);
                case "Contrast": return DynValue.NewNumber(contrast);
            }
            return DynValue.Nil;
        });

        var setter = DynValue.NewCallback((ctx, args) =>
        {
            var keyVal = args[1];
            var val = args[2];
            if (keyVal.Type != DataType.String) { args[0].Table.Set(keyVal, val); return DynValue.Nil; }
            switch (keyVal.String)
            {
                case "Ambient":
                    if (val.UserData?.Object is LuaColor3 ac) { ambient = ac; RenderSettings.ambientLight = new Color(ac.R, ac.G, ac.B); }
                    return DynValue.Nil;
                case "FogColor":
                    if (val.UserData?.Object is LuaColor3 fc) { fogColor = fc; RenderSettings.fogColor = new Color(fc.R, fc.G, fc.B); }
                    return DynValue.Nil;
                case "FogStart":
                    fogStart = (float)val.Number; RenderSettings.fogStartDistance = fogStart; return DynValue.Nil;
                case "FogEnd":
                    fogEnd = (float)val.Number; RenderSettings.fogEndDistance = fogEnd; return DynValue.Nil;
                case "FogEnabled":
                    fogEnabled = val.CastToBool(); RenderSettings.fog = fogEnabled; return DynValue.Nil;
                case "Exposure":
                    exposure = (float)val.Number; return DynValue.Nil;
                case "DirtTexture":
                    dirtTexture = val.UserData?.Object as LuaTexture; return DynValue.Nil;
                case "Skybox":
                    skybox = val.UserData?.Object as LuaTexture; return DynValue.Nil;
                case "BloomThreshold": bloomThreshold = (float)val.Number; return DynValue.Nil;
                case "BloomIntensity": bloomIntensity = (float)val.Number; return DynValue.Nil;
                case "VignetteIntensity": vignetteIntensity = (float)val.Number; return DynValue.Nil;
                case "Saturation": saturation = (float)val.Number; return DynValue.Nil;
                case "Contrast": contrast = (float)val.Number; return DynValue.Nil;
            }
            args[0].Table.Set(keyVal, val);
            return DynValue.Nil;
        });

        var newindexChunk = script.LoadString(
            "return function(self, key, value) (getmetatable(self)).__setter(self, key, value) end",
            null, "Lighting.__newindex");
        var sharedNewIndex = script.Call(newindexChunk);
        mt["__setter"] = setter;
        mt["__newindex"] = sharedNewIndex;

        t.MetaTable = mt;
        return t;
    }

    private static DynValue DoRaycast(Script script, LuaVector3 start, LuaVector3 direction, LuaRaycastParams rp)
    {
        var origin = new Vector3(start.X, start.Y, start.Z);
        var dirRaw = new Vector3(direction.X, direction.Y, direction.Z);

        var dir = dirRaw - origin;
        float distance = dir.magnitude;
        if (distance < 1e-6f)
        {
            dir = dirRaw.normalized;
            distance = rp?.MaxDistance ?? 1000f;
        }
        else
        {
            dir = dir / distance;
        }
        if (rp != null && rp.MaxDistance > 0f) distance = Mathf.Min(distance, rp.MaxDistance);

        var hits = Physics.RaycastAll(origin, dir, distance);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            var h = hits[i];
            var go = h.collider != null ? h.collider.gameObject : null;

            DynValue hitObj = DynValue.Nil;
            string unityName = null;
            var luaInst = ResolveLuaInstanceFor(go);
            var hitPos = new LuaVector3(h.point.x, h.point.y, h.point.z);

            DynValue transformerArg;
            if (luaInst != null)
            {
                hitObj = DynValue.NewTable(luaInst.Table);
                transformerArg = hitObj;
            }
            else
            {
                unityName = go != null ? go.name : "Unknown";
                if (rp != null && rp.IgnoreUnityObjects) continue;
                var t = new Table(script);
                t["ClassName"] = "UnityObject";
                t["CFrame"] = UserData.Create(new LuaCFrame(hitPos, LuaVector3.Zero));
                t["Name"] = unityName;
                transformerArg = DynValue.NewTable(t);
            }

            if (rp?.Transformer != null && rp.Transformer.Type == DataType.Function)
            {
                var ret = script.Call(rp.Transformer, transformerArg);
                if (!ret.CastToBool()) continue;
            }

            var normal = new LuaVector3(h.normal.x, h.normal.y, h.normal.z);
            var result = new LuaRaycastResult(hitPos, normal, h.distance, hitObj, unityName);
            return UserData.Create(result);
        }
        return DynValue.Nil;
    }

    private static LuaInstance ResolveLuaInstanceFor(GameObject go)
    {
        if (go == null || LuaRunner.Instance?.Game == null) return null;
        return SearchTree(LuaRunner.Instance.Game, go);
    }

    private static LuaInstance SearchTree(LuaInstance node, GameObject go)
    {
        if (node.UnityObject == go) return node;
        for (int i = 0; i < node.Children.Count; i++)
        {
            var found = SearchTree(node.Children[i], go);
            if (found != null) return found;
        }
        return null;
    }
}

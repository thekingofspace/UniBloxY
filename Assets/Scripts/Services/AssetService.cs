using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;
using UnityEngine;

public class AssetService : LuaService
{
    public static AssetService Instance { get; private set; }

    private readonly Dictionary<string, LuaShader> shaderCache = new();
    private readonly Dictionary<string, LuaMaterial> materialCache = new();
    private readonly Dictionary<string, LuaTexture> textureCache = new();
    private readonly Dictionary<string, LuaImage> imageCache = new();
    private readonly Dictionary<string, LuaFont> fontCache = new();
    private readonly Dictionary<string, LuaMesh> meshCache = new();
    private readonly Dictionary<string, LuaAudio> audioCache = new();

    private Signal shaderLoadedSignal;
    private Signal assetLoadedSignal;

    public override void Register(Script script)
    {
        lua = script;
        Instance = this;

        UserData.RegisterType<LuaShader>();
        UserData.RegisterType<LuaMaterial>();
        UserData.RegisterType<LuaTexture>();
        UserData.RegisterType<LuaImage>();
        UserData.RegisterType<LuaFont>();
        UserData.RegisterType<LuaMesh>();
        UserData.RegisterType<LuaAudio>();

        shaderLoadedSignal = new Signal(script, "AssetService.ShaderLoaded");
        assetLoadedSignal = new Signal(script, "AssetService.AssetLoaded");

        script.Globals["AssetService"] = BuildTable(script);
    }

    public LuaShader GetShader(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ScriptRuntimeException("AssetService:GetShader requires a shader name");

        if (shaderCache.TryGetValue(name, out var existing))
            return existing;

        var shader = Resources.Load<Shader>("Shaders/" + name);
        if (shader == null) shader = Shader.Find(name);
        if (shader == null)
            throw new ScriptRuntimeException($"AssetService: shader \"{name}\" not found");

        var wrapper = new LuaShader(name, shader);
        shaderCache[name] = wrapper;

        shaderLoadedSignal.Fire(name);
        return wrapper;
    }

    public LuaMaterial GetMaterial(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ScriptRuntimeException("AssetService:GetMaterial requires a material name");

        if (materialCache.TryGetValue(name, out var existing))
            return existing;

        var mat = Resources.Load<Material>("Materials/" + name);
        if (mat == null)
            throw new ScriptRuntimeException($"AssetService: material \"{name}\" not found at Resources/Materials/{name}");

        var wrapper = new LuaMaterial(name, mat);
        materialCache[name] = wrapper;

        assetLoadedSignal.Fire(name);
        return wrapper;
    }

    public LuaMaterial CreateMaterial(string shaderName, string materialName)
    {
        var shader = GetShader(shaderName);
        var mat = new Material(shader.Shader) { name = materialName ?? shaderName };
        var key = materialName ?? ($"_runtime:{shaderName}:{Guid.NewGuid():N}");
        var wrapper = new LuaMaterial(key, mat);
        wrapper.MarkSourceOwned();
        if (!string.IsNullOrEmpty(materialName))
            materialCache[materialName] = wrapper;
        return wrapper;
    }

    public LuaTexture GetTexture(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ScriptRuntimeException("AssetService:GetTexture requires a texture name");

        if (textureCache.TryGetValue(name, out var existing))
            return existing;

        var tex = Resources.Load<Texture>("Textures/" + name);
        if (tex == null) tex = Resources.Load<Texture2D>("Textures/" + name);
        if (tex == null)
            throw new ScriptRuntimeException($"AssetService: texture \"{name}\" not found at Resources/Textures/{name}");

        var wrapper = new LuaTexture(name, tex);
        textureCache[name] = wrapper;

        assetLoadedSignal.Fire(name);
        return wrapper;
    }

    public LuaImage GetImage(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ScriptRuntimeException("AssetService:GetImage requires an image name");

        if (imageCache.TryGetValue(name, out var existing))
            return existing;

        var sprite = Resources.Load<Sprite>("Images/" + name);
        LuaImage wrapper;
        if (sprite != null)
        {
            wrapper = new LuaImage(name, sprite);
        }
        else
        {
            var tex = Resources.Load<Texture2D>("Images/" + name);
            if (tex == null)
                throw new ScriptRuntimeException($"AssetService: image \"{name}\" not found at Resources/Images/{name}");
            wrapper = new LuaImage(name, tex);
        }

        imageCache[name] = wrapper;
        assetLoadedSignal.Fire(name);
        return wrapper;
    }

    public LuaMesh GetMesh(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ScriptRuntimeException("AssetService:GetMesh requires a mesh name");

        if (meshCache.TryGetValue(name, out var existing))
            return existing;

        var mesh = Resources.Load<Mesh>("Meshes/" + name);
        if (mesh == null)
        {
            var prefab = Resources.Load<GameObject>("Meshes/" + name);
            if (prefab != null)
            {
                var mf = prefab.GetComponentInChildren<MeshFilter>();
                if (mf != null) mesh = mf.sharedMesh;
                if (mesh == null)
                {
                    var smr = prefab.GetComponentInChildren<SkinnedMeshRenderer>();
                    if (smr != null) mesh = smr.sharedMesh;
                }
            }
        }
        if (mesh == null)
            throw new ScriptRuntimeException(
                $"AssetService: mesh \"{name}\" not found at Resources/Meshes/{name}");

        var wrapper = new LuaMesh(name, mesh);
        meshCache[name] = wrapper;
        assetLoadedSignal.Fire(name);
        return wrapper;
    }

    public LuaAudio GetSound(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ScriptRuntimeException("AssetService:GetSound requires a sound name");

        if (audioCache.TryGetValue(name, out var existing))
            return existing;

        var clip = Resources.Load<AudioClip>("Sounds/" + name);
        if (clip == null)
            throw new ScriptRuntimeException($"AssetService: sound \"{name}\" not found at Resources/Sounds/{name}");

        var wrapper = new LuaAudio(name, clip);
        audioCache[name] = wrapper;
        assetLoadedSignal.Fire(name);
        return wrapper;
    }

    public LuaFont GetFont(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ScriptRuntimeException("AssetService:GetFont requires a font name");

        if (fontCache.TryGetValue(name, out var existing))
            return existing;

        var font = Resources.Load<Font>("Fonts/" + name);
        if (font == null) font = Resources.GetBuiltinResource<Font>(name);
        if (font == null)
            throw new ScriptRuntimeException($"AssetService: font \"{name}\" not found at Resources/Fonts/{name}");

        var wrapper = new LuaFont(name, font);
        fontCache[name] = wrapper;
        assetLoadedSignal.Fire(name);
        return wrapper;
    }

    private Table BuildTable(Script script)
    {
        var table = new Table(script);

        table["GetShader"] = (Func<string, LuaShader>)GetShader;
        table["Get"] = (Func<string, LuaShader>)GetShader;
        table["GetMaterial"] = (Func<string, LuaMaterial>)GetMaterial;
        table["GetTexture"] = (Func<string, LuaTexture>)GetTexture;
        table["GetImage"] = (Func<string, LuaImage>)GetImage;
        table["GetFont"] = (Func<string, LuaFont>)GetFont;
        table["GetMesh"] = (Func<string, LuaMesh>)GetMesh;
        table["GetSound"] = (Func<string, LuaAudio>)GetSound;
        table["GetAudio"] = (Func<string, LuaAudio>)GetSound;

        table["CreateMaterial"] = DynValue.NewCallback((ctx, args) =>
        {
            if (args.Count < 1 || args[0].Type != DataType.String)
                throw new ScriptRuntimeException("AssetService.CreateMaterial(shaderName [, name])");
            var shaderName = args[0].String;
            var matName = args.Count > 1 && args[1].Type == DataType.String ? args[1].String : null;
            return UserData.Create(CreateMaterial(shaderName, matName));
        });

        table["ShaderLoaded"] = shaderLoadedSignal.BuildTable();
        table["AssetLoaded"] = assetLoadedSignal.BuildTable();

        var mt = new Table(script);
        mt["__type"] = "AssetService";
        table.MetaTable = mt;

        return table;
    }
}

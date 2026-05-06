using System.Collections.Generic;
using MoonSharp.Interpreter;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DataModel : LuaInstanceClass
{
    public override string ClassName => "DataModel";

    public override void Initialize(LuaInstance instance)
    {
        instance.Indestructible = true;
        instance.Reparentable = false;
        instance.IsSceneRoot = true;
        instance.UserState = this;

        instance.Table["ObjectAsInstance"] = DynValue.NewCallback((ctx, args) =>
        {
            var objName = args.Count > 1 ? args[1].String : null;
            var cn = args.Count > 2 ? args[2].String : null;
            if (string.IsNullOrEmpty(objName) || string.IsNullOrEmpty(cn))
                throw new ScriptRuntimeException("ObjectAsInstance(name, className): both required");
            var created = WrapSceneObject(instance, objName, cn);
            return created != null ? DynValue.NewTable(created.Table) : DynValue.Nil;
        });

        instance.Table["ConvertToInstance"] = DynValue.NewCallback((ctx, args) =>
        {
            var entry = args.Count > 1 && args[1].Type == DataType.String ? args[1].String : "";
            var cb = args.Count > 2 ? args[2] : null;
            if (cb == null || cb.Type != DataType.Function)
                throw new ScriptRuntimeException("ConvertToInstance(entryPoint, callback): callback function required");

            GameObject[] roots;
            if (string.IsNullOrEmpty(entry))
            {
                roots = SceneManager.GetActiveScene().GetRootGameObjects();
            }
            else
            {
                var root = GameObject.Find(entry);
                if (root == null) return DynValue.Nil;
                roots = new[] { root };
            }

            var created = new List<LuaInstance>();
            foreach (var go in roots)
                ConvertRecursive(instance, go, instance, cb, created, true);

            return BuildArray(instance.Script, created);
        });

        instance.Table["ImportScene"] = DynValue.NewCallback((ctx, args) =>
        {
            var sceneName = args.Count > 1 && args[1].Type == DataType.String ? args[1].String : null;
            var cb = args.Count > 2 ? args[2] : null;
            if (string.IsNullOrEmpty(sceneName))
                throw new ScriptRuntimeException("ImportScene(sceneName, callback): sceneName required");
            if (cb == null || cb.Type != DataType.Function)
                throw new ScriptRuntimeException("ImportScene(sceneName, callback): callback function required");

            var scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.isLoaded)
            {
                try
                {
                    SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
                }
                catch (System.Exception e)
                {
                    throw new ScriptRuntimeException($"ImportScene: failed to load scene \"{sceneName}\" — {e.Message}");
                }
                scene = SceneManager.GetSceneByName(sceneName);
            }
            if (!scene.IsValid())
                throw new ScriptRuntimeException($"ImportScene: scene \"{sceneName}\" not found (is it in Build Settings?)");

            var created = new List<LuaInstance>();
            foreach (var go in scene.GetRootGameObjects())
                ConvertRecursive(instance, go, instance, cb, created, true);

            return BuildArray(instance.Script, created);
        });
    }

    public override bool TryGetProperty(LuaInstance instance, string key, out DynValue value)
    {
        if (key == "CurrentCamera")
        {
            var cam = instance.FindFirstChildOfClass("Camera");
            value = cam != null ? DynValue.NewTable(cam.Table) : DynValue.Nil;
            return true;
        }
        value = DynValue.Nil;
        return false;
    }

    public void EnsureCamera(LuaInstance gameInstance)
    {
        var cam = gameInstance.FindFirstChildOfClass("Camera");
        if (cam != null) return;
        var def = LuaInstance.GetClass("Camera");
        if (def == null) return;
        var inst = new LuaInstance(gameInstance.Script, "Camera");
        inst.ClassDef = def;
        def.Initialize(inst);
        inst.SetParent(gameInstance);
    }

    private static DynValue BuildArray(Script script, List<LuaInstance> list)
    {
        var t = new Table(script);
        if (list != null)
            for (int i = 0; i < list.Count; i++)
                t[i + 1] = DynValue.NewTable(list[i].Table);
        return DynValue.NewTable(t);
    }

    private void ConvertRecursive(
        LuaInstance gameInstance,
        GameObject go,
        LuaInstance parent,
        DynValue cb,
        List<LuaInstance> topLevel,
        bool topLevelOnly)
    {
        var ret = gameInstance.Script.Call(cb, go.name);
        var nextParent = parent;

        if (ret.Type == DataType.String && !string.IsNullOrEmpty(ret.String))
        {
            var className = ret.String;
            var def = LuaInstance.GetClass(className);
            if (def == null)
                throw new ScriptRuntimeException($"ConvertToInstance: unknown ClassName \"{className}\" for \"{go.name}\"");

            var inst = new LuaInstance(gameInstance.Script, className, go.name);
            inst.ClassDef = def;
            inst.UnityObject = go;
            def.Initialize(inst);
            def.ImportFromUnityObject(inst, go);
            inst.SetParent(parent);
            TransferTags(go, inst);

            nextParent = inst;
            if (!topLevelOnly || parent == gameInstance) topLevel.Add(inst);
        }

        var t = go.transform;
        for (int i = 0; i < t.childCount; i++)
            ConvertRecursive(gameInstance, t.GetChild(i).gameObject, nextParent, cb, topLevel, topLevelOnly);
    }

    private LuaInstance WrapSceneObject(LuaInstance gameInstance, string objName, string className)
    {
        var def = LuaInstance.GetClass(className);
        if (def == null)
            throw new ScriptRuntimeException($"Unknown ClassName \"{className}\"");

        var go = GameObject.Find(objName);
        if (go == null) return null;

        if (go.transform.childCount > 0)
            throw new ScriptRuntimeException(
                $"ObjectAsInstance: \"{objName}\" has {go.transform.childCount} child object(s); " +
                $"use game:ConvertToInstance(\"{objName}\", function(name) ... end) to wrap a subtree.");

        var inst = new LuaInstance(gameInstance.Script, className, objName);
        inst.ClassDef = def;
        inst.UnityObject = go;
        def.Initialize(inst);
        def.ImportFromUnityObject(inst, go);
        inst.SetParent(gameInstance);
        TransferTags(go, inst);
        return inst;
    }

    private static void TransferTags(GameObject go, LuaInstance inst)
    {
        var tagger = go.GetComponent<LuaTags>();
        if (tagger == null) return;
        foreach (var entry in tagger.Entries)
        {
            if (string.IsNullOrEmpty(entry.Key)) continue;
            inst.SetAttribute(entry.Key, DynValue.NewString(entry.Value ?? ""));
        }
    }
}

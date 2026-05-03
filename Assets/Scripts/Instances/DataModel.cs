using MoonSharp.Interpreter;
using UnityEngine;

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
            var wrapper = WrapSceneObject(instance, objName, cn);
            return wrapper != null ? DynValue.NewTable(wrapper.Table) : DynValue.Nil;
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
                roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            }
            else
            {
                var root = GameObject.Find(entry);
                if (root == null) return DynValue.Nil;
                roots = new[] { root };
            }

            var created = new System.Collections.Generic.List<LuaInstance>();
            foreach (var go in roots)
                ConvertRecursive(instance, go, instance, cb, created);

            var result = new Table(instance.Script);
            for (int i = 0; i < created.Count; i++)
                result[i + 1] = DynValue.NewTable(created[i].Table);
            return DynValue.NewTable(result);
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

    private void ConvertRecursive(LuaInstance gameInstance, GameObject go, LuaInstance parent, DynValue cb, System.Collections.Generic.List<LuaInstance> topLevel)
    {
        var ret = gameInstance.Script.Call(cb, go.name);
        var nextParent = parent;
        LuaInstance created = null;

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

            created = inst;
            nextParent = inst;
            if (parent == gameInstance) topLevel.Add(inst);
        }

        var t = go.transform;
        for (int i = 0; i < t.childCount; i++)
            ConvertRecursive(gameInstance, t.GetChild(i).gameObject, nextParent, cb, topLevel);
    }

    private LuaInstance WrapSceneObject(LuaInstance gameInstance, string objName, string className)
    {
        var def = LuaInstance.GetClass(className);
        if (def == null)
            throw new ScriptRuntimeException($"Unknown ClassName \"{className}\"");

        var go = GameObject.Find(objName);
        if (go == null) return null;

        var inst = new LuaInstance(gameInstance.Script, className, objName);
        inst.ClassDef = def;
        inst.UnityObject = go;
        def.Initialize(inst);
        def.ImportFromUnityObject(inst, go);
        inst.SetParent(gameInstance);
        return inst;
    }
}

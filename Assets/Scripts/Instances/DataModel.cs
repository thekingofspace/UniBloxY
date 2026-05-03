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

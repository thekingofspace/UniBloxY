using MoonSharp.Interpreter;
using UnityEngine;

public abstract class LuaService : MonoBehaviour
{
    protected Script lua;

    public abstract void Register(Script script);

    protected void Spawn(Closure callback, string label, params object[] args)
    {
        try
        {
            var co = lua.CreateCoroutine(DynValue.NewClosure(callback));
            co.Coroutine.Resume(args);
        }
        catch (ScriptRuntimeException ex)
        {
            Debug.LogError($"{label}: {ex.DecoratedMessage}");
        }
    }
}

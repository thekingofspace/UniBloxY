using System.Collections.Generic;
using MoonSharp.Interpreter;
using UnityEngine;
using LuaCoroutine = MoonSharp.Interpreter.Coroutine;

[MoonSharpUserData]
public class SignalConnection
{
    private readonly Signal signal;
    internal Closure Callback;
    internal bool IsOnce;
    public bool Connected { get; private set; } = true;
    public string ClassName => "SignalConnection";

    internal SignalConnection(Signal s, Closure cb, bool once)
    {
        signal = s;
        Callback = cb;
        IsOnce = once;
    }

    public void Disconnect()
    {
        if (!Connected) return;
        Connected = false;
        signal.Remove(this);
    }

    // Used by Signal.Clear to flag every connection as severed in one pass
    // without each one re-entering Signal.Remove on the list we're clearing.
    internal void MarkDisconnected()
    {
        Connected = false;
        Callback = null;
    }
}

public class Signal
{
    private static bool typesRegistered;

    private readonly Script script;
    private readonly string label;
    private readonly List<SignalConnection> connections = new List<SignalConnection>();
    private readonly List<LuaCoroutine> waiters = new List<LuaCoroutine>();

    // Reused snapshot to avoid allocating a fresh array on every Fire. A flag
    // guards against reentrancy — if a callback fires the same signal again,
    // the inner call falls back to a one-shot list so it doesn't clobber the
    // outer iteration.
    private readonly List<SignalConnection> firingBuffer = new List<SignalConnection>();
    private bool firingBufferInUse;

    public Signal(Script script, string label)
    {
        this.script = script;
        this.label = label;
        if (!typesRegistered)
        {
            UserData.RegisterType<SignalConnection>();
            typesRegistered = true;
        }
    }

    private DynValue Connect(Closure cb)
    {
        if (cb == null)
        {
            Debug.LogError($"{label}:Connect callback is nil");
            return DynValue.Nil;
        }
        var c = new SignalConnection(this, cb, false);
        connections.Add(c);
        return UserData.Create(c);
    }

    private DynValue Once(Closure cb)
    {
        if (cb == null)
        {
            Debug.LogError($"{label}:Once callback is nil");
            return DynValue.Nil;
        }
        var c = new SignalConnection(this, cb, true);
        connections.Add(c);
        return UserData.Create(c);
    }

    private void AddWaiter(DynValue thread)
    {
        if (thread != null && thread.Type == DataType.Thread)
            waiters.Add(thread.Coroutine);
    }

    internal void Remove(SignalConnection c)
    {
        connections.Remove(c);
    }

    // Drops every active connection and pending waiter. Called when the owning
    // instance is destroyed so callbacks (and any state they captured) can be
    // collected even if external Lua code still holds a reference to the
    // destroyed instance.
    public void Clear()
    {
        for (int i = 0; i < connections.Count; i++)
            connections[i].MarkDisconnected();
        connections.Clear();
        waiters.Clear();
    }

    public void Fire(params object[] args)
    {
        if (connections.Count > 0)
        {
            List<SignalConnection> snap;
            bool ownsBuffer = !firingBufferInUse;
            if (ownsBuffer)
            {
                snap = firingBuffer;
                snap.Clear();
                firingBufferInUse = true;
            }
            else
            {
                snap = new List<SignalConnection>(connections.Count);
            }

            try
            {
                for (int i = 0; i < connections.Count; i++) snap.Add(connections[i]);
                int count = snap.Count;
                for (int i = 0; i < count; i++)
                {
                    var c = snap[i];
                    if (!c.Connected) continue;
                    if (c.IsOnce) c.Disconnect();
                    try
                    {
                        var co = script.CreateCoroutine(DynValue.NewClosure(c.Callback));
                        co.Coroutine.Resume(args);
                    }
                    catch (ScriptRuntimeException ex)
                    {
                        Debug.LogError($"{label}: {ex.DecoratedMessage}");
                    }
                }
            }
            finally
            {
                if (ownsBuffer)
                {
                    snap.Clear();
                    firingBufferInUse = false;
                }
            }
        }

        if (waiters.Count > 0)
        {
            var snapshot = waiters.ToArray();
            waiters.Clear();
            for (int i = 0; i < snapshot.Length; i++)
            {
                try
                {
                    snapshot[i].Resume(args);
                }
                catch (ScriptRuntimeException ex)
                {
                    Debug.LogError($"{label} Wait: {ex.DecoratedMessage}");
                }
            }
        }
    }

    public Table BuildTable()
    {
        var t = new Table(script);

        t["Connect"] = DynValue.NewCallback((ctx, args) =>
        {
            var self = args[0];
            var cb = args[1].Function;
            return Connect(cb);
        });

        t["Once"] = DynValue.NewCallback((ctx, args) =>
        {
            var self = args[0];
            var cb = args[1].Function;
            return Once(cb);
        });

        t["_addWaiter"] = DynValue.NewCallback((ctx, args) =>
        {
            var self = args[0];
            var th = args[1];
            AddWaiter(th);
            return DynValue.Nil;
        });

        var chunk = script.LoadString(
            "return function(self) self:_addWaiter(coroutine.running()) return coroutine.yield() end",
            null, $"{label}.Wait");

        t["Wait"] = script.Call(chunk);

        var mt = new Table(script);
        mt["__type"] = "Signal";
        t.MetaTable = mt;

        return t;
    }
}

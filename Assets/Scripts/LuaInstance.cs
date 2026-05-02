using System;
using System.Collections.Generic;
using System.Linq;
using MoonSharp.Interpreter;
using UnityEngine;

public class LuaInstance
{
    private static bool registered;
    private static readonly Dictionary<string, LuaInstanceClass> classRegistry = new();
    private static DynValue sharedNewIndex;

    public static void RegisterClass(LuaInstanceClass def)
    {
        classRegistry[def.ClassName] = def;
    }

    public static LuaInstanceClass GetClass(string className)
    {
        classRegistry.TryGetValue(className, out var def);
        return def;
    }

    private readonly Script script;
    private readonly Table table;

    private string name;
    private readonly string className;
    private LuaInstance parent;
    private readonly List<LuaInstance> children = new();

    private readonly Dictionary<string, DynValue> attributes = new();

    private readonly Signal changed;
    private Table changedTable;
    private readonly Dictionary<string, Signal> propertyChangedSignals = new();
    private readonly Dictionary<string, Table> propertyChangedTables = new();
    private readonly Dictionary<string, Signal> attributeChangedSignals = new();
    private readonly Dictionary<string, Table> attributeChangedTables = new();
    private Signal childAdded; private Table childAddedTable;
    private Signal childRemoved; private Table childRemovedTable;
    private Signal ancestryChanged; private Table ancestryChangedTable;
    private Signal attributeChangedAny; private Table attributeChangedAnyTable;
    private Signal destroying; private Table destroyingTable;

    private bool destroyed;
    private bool inScene;

    public string Name => name;
    public string ClassName => className;
    public LuaInstance Parent => parent;
    public Table Table => table;
    public Script Script => script;
    public IReadOnlyList<LuaInstance> Children => children;

    public GameObject UnityObject { get; set; }
    public LuaInstanceClass ClassDef { get; set; }
    public object UserState { get; set; }

    public bool Indestructible { get; set; }
    public bool Reparentable { get; set; } = true;
    public bool IsSceneRoot { get; set; }

    public bool InScene => inScene;

    public LuaInstance(Script script, string className, string name = null)
    {
        this.script = script;
        this.className = className;
        this.name = name ?? className;

        changed = new Signal(script, $"{className}.Changed");

        table = BuildTable();
    }

    public static void EnsureRegistered(Script script)
    {
        if (registered) return;
        registered = true;

        UserData.RegisterType<LuaInstance>();

        var newindexChunk = script.LoadString(
            "return function(self, key, value) (getmetatable(self)).__setter(self, key, value) end",
            null, "Instance.__newindex");
        sharedNewIndex = script.Call(newindexChunk);

        var classTypes = typeof(LuaInstanceClass).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && typeof(LuaInstanceClass).IsAssignableFrom(t));
        foreach (var t in classTypes)
        {
            var def = (LuaInstanceClass)Activator.CreateInstance(t);
            RegisterClass(def);
        }

        var lib = new Table(script);
        Func<string, DynValue, DynValue> ctor = (cn, parentVal) =>
        {
            if (string.IsNullOrEmpty(cn))
                throw new ScriptRuntimeException("Instance.new: ClassName must be a string");
            if (!classRegistry.TryGetValue(cn, out var def))
                throw new ScriptRuntimeException($"Unable to create an Instance of type \"{cn}\"");
            var inst = new LuaInstance(script, cn);
            inst.ClassDef = def;
            def.Initialize(inst);
            if (parentVal != null && parentVal.Type == DataType.Table)
            {
                var p = ResolveInstance(parentVal);
                if (p != null) inst.SetParent(p);
            }
            return DynValue.NewTable(inst.table);
        };
        lib["new"] = ctor;
        lib["New"] = ctor;
        script.Globals["Instance"] = lib;
    }

    public static LuaInstance ResolveInstance(DynValue v)
    {
        if (v == null || v.Type != DataType.Table) return null;
        var raw = v.Table.RawGet("__instance");
        if (raw.Type == DataType.UserData && raw.UserData.Object is LuaInstance li)
            return li;
        return null;
    }

    private Table BuildTable()
    {
        var t = new Table(script);

        t["__instance"] = UserData.Create(this);

        t["GetPropertyChangedSignal"] = DynValue.NewCallback((ctx, args) =>
        {
            var prop = args.Count > 1 ? args[1].String : null;
            if (string.IsNullOrEmpty(prop)) return DynValue.Nil;
            if (!propertyChangedTables.TryGetValue(prop, out var tbl))
            {
                var sig = new Signal(script, $"{className}.{prop}Changed");
                propertyChangedSignals[prop] = sig;
                tbl = sig.BuildTable();
                propertyChangedTables[prop] = tbl;
            }
            return DynValue.NewTable(tbl);
        });

        t["GetAttributeChangedSignal"] = DynValue.NewCallback((ctx, args) =>
        {
            var attr = args.Count > 1 ? args[1].String : null;
            if (string.IsNullOrEmpty(attr)) return DynValue.Nil;
            if (!attributeChangedTables.TryGetValue(attr, out var tbl))
            {
                var sig = new Signal(script, $"{className}.{attr}AttributeChanged");
                attributeChangedSignals[attr] = sig;
                tbl = sig.BuildTable();
                attributeChangedTables[attr] = tbl;
            }
            return DynValue.NewTable(tbl);
        });

        t["GetAttribute"] = DynValue.NewCallback((ctx, args) =>
        {
            var key = args.Count > 1 ? args[1].String : null;
            if (key == null) return DynValue.Nil;
            return attributes.TryGetValue(key, out var v) ? v : DynValue.Nil;
        });

        t["SetAttribute"] = DynValue.NewCallback((ctx, args) =>
        {
            var key = args.Count > 1 ? args[1].String : null;
            if (key == null) return DynValue.Nil;
            var val = args.Count > 2 ? args[2] : DynValue.Nil;
            SetAttribute(key, val);
            return DynValue.Nil;
        });

        t["GetAttributes"] = DynValue.NewCallback((ctx, args) =>
        {
            var tbl = new Table(script);
            foreach (var kv in attributes)
                tbl[kv.Key] = kv.Value;
            return DynValue.NewTable(tbl);
        });

        t["FindFirstChild"] = DynValue.NewCallback((ctx, args) =>
        {
            var n = args.Count > 1 ? args[1].String : null;
            var recursive = args.Count > 2 && args[2].CastToBool();
            if (n == null) return DynValue.Nil;
            var found = FindFirstChild(n, recursive);
            return found != null ? DynValue.NewTable(found.table) : DynValue.Nil;
        });

        t["FindFirstAncestor"] = DynValue.NewCallback((ctx, args) =>
        {
            var n = args.Count > 1 ? args[1].String : null;
            if (n == null) return DynValue.Nil;
            var p = parent;
            while (p != null)
            {
                if (p.name == n) return DynValue.NewTable(p.table);
                p = p.parent;
            }
            return DynValue.Nil;
        });

        t["GetChildren"] = DynValue.NewCallback((ctx, args) =>
        {
            var tbl = new Table(script);
            for (int i = 0; i < children.Count; i++)
                tbl[i + 1] = DynValue.NewTable(children[i].table);
            return DynValue.NewTable(tbl);
        });

        t["GetDescendants"] = DynValue.NewCallback((ctx, args) =>
        {
            var tbl = new Table(script);
            int idx = 1;
            CollectDescendants(this, tbl, ref idx);
            return DynValue.NewTable(tbl);
        });

        t["IsDescendantOf"] = DynValue.NewCallback((ctx, args) =>
        {
            var other = args.Count > 1 ? ResolveInstance(args[1]) : null;
            return DynValue.NewBoolean(IsDescendantOf(other));
        });

        t["IsAncestorOf"] = DynValue.NewCallback((ctx, args) =>
        {
            var other = args.Count > 1 ? ResolveInstance(args[1]) : null;
            return DynValue.NewBoolean(other != null && other.IsDescendantOf(this));
        });

        t["ClearAllChildren"] = DynValue.NewCallback((ctx, args) =>
        {
            var snap = children.ToArray();
            for (int i = 0; i < snap.Length; i++) snap[i].Destroy();
            return DynValue.Nil;
        });

        t["Destroy"] = DynValue.NewCallback((ctx, args) => { Destroy(); return DynValue.Nil; });

        t["Clone"] = DynValue.NewCallback((ctx, args) => DynValue.NewTable(Clone().table));

        var mt = new Table(script);
        mt["__type"] = className;

        mt["__index"] = (Func<DynValue, DynValue, DynValue>)((_, key) =>
        {
            if (key.Type != DataType.String) return DynValue.Nil;
            var k = key.String;
            switch (k)
            {
                case "Name": return DynValue.NewString(name);
                case "ClassName": return DynValue.NewString(className);
                case "Parent":
                    return parent != null ? DynValue.NewTable(parent.table) : DynValue.Nil;
                case "Changed":
                    changedTable ??= changed.BuildTable();
                    return DynValue.NewTable(changedTable);
                case "ChildAdded":
                    if (childAdded == null) { childAdded = new Signal(script, $"{className}.ChildAdded"); childAddedTable = childAdded.BuildTable(); }
                    return DynValue.NewTable(childAddedTable);
                case "ChildRemoved":
                    if (childRemoved == null) { childRemoved = new Signal(script, $"{className}.ChildRemoved"); childRemovedTable = childRemoved.BuildTable(); }
                    return DynValue.NewTable(childRemovedTable);
                case "AncestryChanged":
                    if (ancestryChanged == null) { ancestryChanged = new Signal(script, $"{className}.AncestryChanged"); ancestryChangedTable = ancestryChanged.BuildTable(); }
                    return DynValue.NewTable(ancestryChangedTable);
                case "AttributeChanged":
                    if (attributeChangedAny == null) { attributeChangedAny = new Signal(script, $"{className}.AttributeChanged"); attributeChangedAnyTable = attributeChangedAny.BuildTable(); }
                    return DynValue.NewTable(attributeChangedAnyTable);
                case "Destroying":
                    if (destroying == null) { destroying = new Signal(script, $"{className}.Destroying"); destroyingTable = destroying.BuildTable(); }
                    return DynValue.NewTable(destroyingTable);
            }
            if (ClassDef != null && ClassDef.TryGetProperty(this, k, out var classVal))
                return classVal;
            var child = FindFirstChild(k, false);
            if (child != null) return DynValue.NewTable(child.table);
            return DynValue.Nil;
        });

        var setter = DynValue.NewCallback((ctx, args) =>
        {
            var selfVal = args[0];
            var keyVal = args[1];
            var val = args[2];
            if (keyVal.Type != DataType.String)
            {
                selfVal.Table.Set(keyVal, val);
                return DynValue.Nil;
            }
            var k = keyVal.String;
            switch (k)
            {
                case "Name":
                    if (val.Type == DataType.String) SetName(val.String);
                    return DynValue.Nil;
                case "Parent":
                    if (val.IsNil()) SetParent(null);
                    else if (val.Type == DataType.Table)
                    {
                        var p = ResolveInstance(val);
                        if (p == null) throw new ScriptRuntimeException("Parent must be an Instance or nil");
                        SetParent(p);
                    }
                    else throw new ScriptRuntimeException("Parent must be an Instance or nil");
                    return DynValue.Nil;
                case "ClassName":
                    throw new ScriptRuntimeException("ClassName is read-only");
            }
            if (ClassDef != null && ClassDef.TrySetProperty(this, k, val))
            {
                FirePropertyChanged(k);
                return DynValue.Nil;
            }
            selfVal.Table.Set(keyVal, val);
            return DynValue.Nil;
        });

        mt["__setter"] = setter;
        mt["__newindex"] = sharedNewIndex;

        mt["__tostring"] = (Func<DynValue, string>)(_ => name);

        t.MetaTable = mt;
        return t;
    }

    private static void CollectDescendants(LuaInstance node, Table tbl, ref int idx)
    {
        for (int i = 0; i < node.children.Count; i++)
        {
            var c = node.children[i];
            tbl[idx++] = DynValue.NewTable(c.table);
            CollectDescendants(c, tbl, ref idx);
        }
    }

    public LuaInstance FindFirstChild(string n, bool recursive)
    {
        for (int i = 0; i < children.Count; i++)
            if (children[i].name == n) return children[i];
        if (!recursive) return null;
        for (int i = 0; i < children.Count; i++)
        {
            var found = children[i].FindFirstChild(n, true);
            if (found != null) return found;
        }
        return null;
    }

    public LuaInstance FindFirstChildOfClass(string cn)
    {
        for (int i = 0; i < children.Count; i++)
            if (children[i].className == cn) return children[i];
        return null;
    }

    public bool IsDescendantOf(LuaInstance other)
    {
        if (other == null) return false;
        var p = parent;
        while (p != null)
        {
            if (p == other) return true;
            p = p.parent;
        }
        return false;
    }

    public void SetName(string newName)
    {
        if (name == newName) return;
        name = newName;
        if (UnityObject != null) UnityObject.name = newName;
        FirePropertyChanged("Name");
    }

    public void FirePropertyChanged(string prop)
    {
        if (propertyChangedSignals.TryGetValue(prop, out var sig)) sig.Fire();
        changed.Fire(prop);
    }

    public void SetParent(LuaInstance newParent)
    {
        if (destroyed && newParent != null)
            throw new ScriptRuntimeException($"Cannot set Parent of destroyed instance");
        if (newParent == parent)
        {
            if (newParent != null && newParent.IsSceneRoot)
                newParent.ClassDef?.OnChildAdded(newParent, this);
            return;
        }

        if (!Reparentable && parent != null)
            throw new ScriptRuntimeException($"{className} \"{name}\" cannot be reparented");

        var p = newParent;
        while (p != null)
        {
            if (p == this) throw new ScriptRuntimeException("Attempt to set parent would result in circular reference");
            p = p.parent;
        }

        var old = parent;
        if (old != null)
        {
            old.children.Remove(this);
            old.childRemoved?.Fire(table);
            old.ClassDef?.OnChildRemoved(old, this);
        }

        parent = newParent;

        if (newParent != null)
        {
            newParent.children.Add(this);
            newParent.childAdded?.Fire(table);
            newParent.ClassDef?.OnChildAdded(newParent, this);
        }

        UpdateInSceneRecursive(this);

        if (UnityObject != null)
        {
            var newUnityParent = newParent?.UnityObject;
            UnityObject.transform.SetParent(newUnityParent != null ? newUnityParent.transform : null, true);
        }

        FirePropertyChanged("Parent");
        FireAncestryChangedRecursive(this, newParent);
    }

    private static bool ComputeInScene(LuaInstance node)
    {
        var p = node;
        while (p != null)
        {
            if (p.IsSceneRoot) return true;
            p = p.parent;
        }
        return false;
    }

    private static void UpdateInSceneRecursive(LuaInstance node)
    {
        var newInScene = ComputeInScene(node);
        if (newInScene != node.inScene)
        {
            node.inScene = newInScene;
            if (newInScene) node.ClassDef?.OnEnterScene(node);
            else node.ClassDef?.OnExitScene(node);
        }
        for (int i = 0; i < node.children.Count; i++)
            UpdateInSceneRecursive(node.children[i]);
    }

    public void ForceEnterScene()
    {
        if (inScene) return;
        inScene = true;
        ClassDef?.OnEnterScene(this);
        for (int i = 0; i < children.Count; i++)
            UpdateInSceneRecursive(children[i]);
    }

    private static void FireAncestryChangedRecursive(LuaInstance node, LuaInstance newParent)
    {
        node.ancestryChanged?.Fire(node.table, newParent != null ? (object)newParent.table : null);
        for (int i = 0; i < node.children.Count; i++)
            FireAncestryChangedRecursive(node.children[i], node.children[i].parent);
    }

    public void SetAttribute(string key, DynValue val)
    {
        if (val == null || val.IsNil()) attributes.Remove(key);
        else attributes[key] = val;
        if (attributeChangedSignals.TryGetValue(key, out var sig)) sig.Fire();
        attributeChangedAny?.Fire(key);
    }

    public LuaInstance Clone()
    {
        var copy = new LuaInstance(script, className, name);
        copy.ClassDef = ClassDef;
        ClassDef?.Initialize(copy);
        foreach (var kv in attributes) copy.attributes[kv.Key] = kv.Value;
        for (int i = 0; i < children.Count; i++)
        {
            var childCopy = children[i].Clone();
            childCopy.SetParent(copy);
        }
        return copy;
    }

    public void Destroy()
    {
        if (destroyed) return;
        if (Indestructible)
            throw new ScriptRuntimeException($"{className} \"{name}\" cannot be destroyed");
        destroying?.Fire(table);
        destroyed = true;
        SetParent(null);
        var snap = children.ToArray();
        for (int i = 0; i < snap.Length; i++) snap[i].Destroy();
        if (UnityObject != null)
        {
            UnityEngine.Object.Destroy(UnityObject);
            UnityObject = null;
        }
    }
}

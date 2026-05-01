using System;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class LuaVector2
{
    public float X { get; }
    public float Y { get; }

    public LuaVector2(float x, float y) { X = x; Y = y; }

    [MoonSharpHidden] public static readonly LuaVector2 Zero = new LuaVector2(0f, 0f);
    [MoonSharpHidden] public static readonly LuaVector2 One = new LuaVector2(1f, 1f);

    public override string ToString() => $"{X}, {Y}";
}

[MoonSharpUserData]
public class LuaVector3
{
    public float X { get; }
    public float Y { get; }
    public float Z { get; }

    public LuaVector3(float x, float y, float z) { X = x; Y = y; Z = z; }

    [MoonSharpHidden] public static readonly LuaVector3 Zero = new LuaVector3(0f, 0f, 0f);
    [MoonSharpHidden] public static readonly LuaVector3 One = new LuaVector3(1f, 1f, 1f);

    public override string ToString() => $"{X}, {Y}, {Z}";
}

[MoonSharpUserData]
public class LuaColor3
{
    public float R { get; }
    public float G { get; }
    public float B { get; }

    public LuaColor3(float r, float g, float b) { R = r; G = g; B = b; }

    public override string ToString() => $"{R}, {G}, {B}";
}

[MoonSharpUserData]
public class LuaUDim
{
    public float Scale { get; }
    public float Offset { get; }

    public LuaUDim(float scale, float offset) { Scale = scale; Offset = offset; }

    public override string ToString() => $"{Scale}, {Offset}";
}

[MoonSharpUserData]
public class LuaUDim2
{
    public LuaUDim X { get; }
    public LuaUDim Y { get; }

    public LuaUDim2(LuaUDim x, LuaUDim y) { X = x; Y = y; }

    public override string ToString() => $"({X.Scale}, {X.Offset}), ({Y.Scale}, {Y.Offset})";
}

[MoonSharpUserData]
public class LuaCFrame
{
    public LuaVector3 Position { get; }
    public LuaVector3 Rotation { get; }

    public LuaCFrame(LuaVector3 position, LuaVector3 rotation)
    {
        Position = position;
        Rotation = rotation;
    }

    public override string ToString() => $"Position({Position}), Rotation({Rotation})";
}

public static class LuaTypes
{
    public static void Register(Script script)
    {
        UserData.RegisterType<LuaVector2>();
        UserData.RegisterType<LuaVector3>();
        UserData.RegisterType<LuaColor3>();
        UserData.RegisterType<LuaUDim>();
        UserData.RegisterType<LuaUDim2>();
        UserData.RegisterType<LuaCFrame>();

        script.Globals["Vector2"] = BuildVector2(script);
        script.Globals["Vector3"] = BuildVector3(script);
        script.Globals["Color3"] = BuildColor3(script);
        script.Globals["UDim"] = BuildUDim(script);
        script.Globals["UDim2"] = BuildUDim2(script);
        script.Globals["CFrame"] = BuildCFrame(script);
    }

    private static Table BuildVector2(Script s)
    {
        var t = new Table(s);
        t["new"] = (Func<float, float, LuaVector2>)((x, y) => new LuaVector2(x, y));
        t["zero"] = LuaVector2.Zero;
        t["one"] = LuaVector2.One;
        return t;
    }

    private static Table BuildVector3(Script s)
    {
        var t = new Table(s);
        t["new"] = (Func<float, float, float, LuaVector3>)((x, y, z) => new LuaVector3(x, y, z));
        t["zero"] = LuaVector3.Zero;
        t["one"] = LuaVector3.One;
        return t;
    }

    private static Table BuildColor3(Script s)
    {
        var t = new Table(s);
        t["new"] = (Func<float, float, float, LuaColor3>)((r, g, b) => new LuaColor3(r, g, b));
        t["fromRGB"] = (Func<int, int, int, LuaColor3>)((r, g, b) =>
            new LuaColor3(r / 255f, g / 255f, b / 255f));
        return t;
    }

    private static Table BuildUDim(Script s)
    {
        var t = new Table(s);
        t["new"] = (Func<float, float, LuaUDim>)((scale, offset) => new LuaUDim(scale, offset));
        return t;
    }

    private static Table BuildUDim2(Script s)
    {
        var t = new Table(s);
        t["new"] = (Func<float, float, float, float, LuaUDim2>)((xs, xo, ys, yo) =>
            new LuaUDim2(new LuaUDim(xs, xo), new LuaUDim(ys, yo)));
        t["fromScale"] = (Func<float, float, LuaUDim2>)((x, y) =>
            new LuaUDim2(new LuaUDim(x, 0f), new LuaUDim(y, 0f)));
        t["fromOffset"] = (Func<float, float, LuaUDim2>)((x, y) =>
            new LuaUDim2(new LuaUDim(0f, x), new LuaUDim(0f, y)));
        return t;
    }

    private static Table BuildCFrame(Script s)
    {
        var t = new Table(s);
        t["new"] = (Func<float, float, float, LuaCFrame>)((x, y, z) =>
            new LuaCFrame(new LuaVector3(x, y, z), LuaVector3.Zero));
        t["fromPosition"] = (Func<LuaVector3, LuaCFrame>)((pos) =>
            new LuaCFrame(pos, LuaVector3.Zero));
        t["fromEulerAngles"] = (Func<LuaVector3, LuaVector3, LuaCFrame>)((pos, rot) =>
            new LuaCFrame(pos, rot));
        return t;
    }
}

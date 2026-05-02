using System;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class LuaVector2
{
    public float X { get; }
    public float Y { get; }
    public string ClassName => "Vector2";

    public LuaVector2(float x, float y) { X = x; Y = y; }

    [MoonSharpHidden] public static readonly LuaVector2 Zero = new LuaVector2(0f, 0f);
    [MoonSharpHidden] public static readonly LuaVector2 One = new LuaVector2(1f, 1f);

    public static LuaVector2 operator +(LuaVector2 a, LuaVector2 b) => new LuaVector2(a.X + b.X, a.Y + b.Y);
    public static LuaVector2 operator -(LuaVector2 a, LuaVector2 b) => new LuaVector2(a.X - b.X, a.Y - b.Y);
    public static LuaVector2 operator -(LuaVector2 a) => new LuaVector2(-a.X, -a.Y);
    public static LuaVector2 operator *(LuaVector2 a, float b) => new LuaVector2(a.X * b, a.Y * b);
    public static LuaVector2 operator *(float a, LuaVector2 b) => new LuaVector2(a * b.X, a * b.Y);
    public static LuaVector2 operator /(LuaVector2 a, float b) => new LuaVector2(a.X / b, a.Y / b);

    public LuaVector2 Lerp(LuaVector2 other, float t) =>
        new LuaVector2(X + (other.X - X) * t, Y + (other.Y - Y) * t);

    public override string ToString() => $"{X}, {Y}";
}

[MoonSharpUserData]
public class LuaVector3
{
    public float X { get; }
    public float Y { get; }
    public float Z { get; }
    public string ClassName => "Vector3";

    public LuaVector3(float x, float y, float z) { X = x; Y = y; Z = z; }

    [MoonSharpHidden] public static readonly LuaVector3 Zero = new LuaVector3(0f, 0f, 0f);
    [MoonSharpHidden] public static readonly LuaVector3 One = new LuaVector3(1f, 1f, 1f);

    public static LuaVector3 operator +(LuaVector3 a, LuaVector3 b) => new LuaVector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static LuaVector3 operator -(LuaVector3 a, LuaVector3 b) => new LuaVector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static LuaVector3 operator -(LuaVector3 a) => new LuaVector3(-a.X, -a.Y, -a.Z);
    public static LuaVector3 operator *(LuaVector3 a, float b) => new LuaVector3(a.X * b, a.Y * b, a.Z * b);
    public static LuaVector3 operator *(float a, LuaVector3 b) => new LuaVector3(a * b.X, a * b.Y, a * b.Z);
    public static LuaVector3 operator /(LuaVector3 a, float b) => new LuaVector3(a.X / b, a.Y / b, a.Z / b);

    public LuaVector3 Lerp(LuaVector3 other, float t) =>
        new LuaVector3(X + (other.X - X) * t, Y + (other.Y - Y) * t, Z + (other.Z - Z) * t);

    public override string ToString() => $"{X}, {Y}, {Z}";
}

[MoonSharpUserData]
public class LuaColor3
{
    public float R { get; }
    public float G { get; }
    public float B { get; }
    public string ClassName => "Color3";

    public LuaColor3(float r, float g, float b) { R = r; G = g; B = b; }

    public static LuaColor3 operator *(LuaColor3 a, float b) => new LuaColor3(a.R * b, a.G * b, a.B * b);
    public static LuaColor3 operator *(float a, LuaColor3 b) => new LuaColor3(a * b.R, a * b.G, a * b.B);

    public LuaColor3 Lerp(LuaColor3 other, float t) =>
        new LuaColor3(R + (other.R - R) * t, G + (other.G - G) * t, B + (other.B - B) * t);

    public override string ToString() => $"{R}, {G}, {B}";
}

[MoonSharpUserData]
public class LuaUDim
{
    public float Scale { get; }
    public float Offset { get; }
    public string ClassName => "UDim";

    public LuaUDim(float scale, float offset) { Scale = scale; Offset = offset; }

    public LuaUDim Lerp(LuaUDim other, float t) =>
        new LuaUDim(Scale + (other.Scale - Scale) * t, Offset + (other.Offset - Offset) * t);

    public override string ToString() => $"{Scale}, {Offset}";
}

[MoonSharpUserData]
public class LuaUDim2
{
    public LuaUDim X { get; }
    public LuaUDim Y { get; }
    public string ClassName => "UDim2";

    public LuaUDim2(LuaUDim x, LuaUDim y) { X = x; Y = y; }

    public static LuaUDim2 operator +(LuaUDim2 a, LuaUDim2 b) =>
        new LuaUDim2(new LuaUDim(a.X.Scale + b.X.Scale, a.X.Offset + b.X.Offset),
                     new LuaUDim(a.Y.Scale + b.Y.Scale, a.Y.Offset + b.Y.Offset));
    public static LuaUDim2 operator -(LuaUDim2 a, LuaUDim2 b) =>
        new LuaUDim2(new LuaUDim(a.X.Scale - b.X.Scale, a.X.Offset - b.X.Offset),
                     new LuaUDim(a.Y.Scale - b.Y.Scale, a.Y.Offset - b.Y.Offset));

    public LuaUDim2 Lerp(LuaUDim2 other, float t) =>
        new LuaUDim2(X.Lerp(other.X, t), Y.Lerp(other.Y, t));

    public override string ToString() => $"({X.Scale}, {X.Offset}), ({Y.Scale}, {Y.Offset})";
}

[MoonSharpUserData]
public class LuaCFrame
{
    public LuaVector3 Position { get; }
    public LuaVector3 Rotation { get; }
    public LuaVector3 Angles => Rotation;
    public string ClassName => "CFrame";

    public LuaCFrame(LuaVector3 position, LuaVector3 rotation)
    {
        Position = position;
        Rotation = rotation;
    }

    public LuaCFrame Lerp(LuaCFrame other, float t) =>
        new LuaCFrame(Position.Lerp(other.Position, t), Rotation.Lerp(other.Rotation, t));

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
        t["Angles"] = (Func<float, float, float, LuaCFrame>)((rx, ry, rz) =>
            new LuaCFrame(LuaVector3.Zero, new LuaVector3(rx, ry, rz)));
        return t;
    }
}

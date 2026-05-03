using System;
using MoonSharp.Interpreter;
using UnityEngine;

[MoonSharpUserData]
public class LuaVector2
{
    public float X { get; }
    public float Y { get; }
    public string ClassName => "Vector2";

    public LuaVector2(float x, float y) { X = x; Y = y; }

    [MoonSharpHidden] public static readonly LuaVector2 Zero = new LuaVector2(0f, 0f);
    [MoonSharpHidden] public static readonly LuaVector2 One = new LuaVector2(1f, 1f);
    [MoonSharpHidden] public static readonly LuaVector2 XAxis = new LuaVector2(1f, 0f);
    [MoonSharpHidden] public static readonly LuaVector2 YAxis = new LuaVector2(0f, 1f);

    public float Magnitude => Mathf.Sqrt(X * X + Y * Y);
    public float SquaredMagnitude => X * X + Y * Y;
    public LuaVector2 Unit
    {
        get
        {
            var m = Magnitude;
            return m == 0f ? Zero : new LuaVector2(X / m, Y / m);
        }
    }

    public static LuaVector2 operator +(LuaVector2 a, LuaVector2 b) => new LuaVector2(a.X + b.X, a.Y + b.Y);
    public static LuaVector2 operator -(LuaVector2 a, LuaVector2 b) => new LuaVector2(a.X - b.X, a.Y - b.Y);
    public static LuaVector2 operator -(LuaVector2 a) => new LuaVector2(-a.X, -a.Y);
    public static LuaVector2 operator *(LuaVector2 a, LuaVector2 b) => new LuaVector2(a.X * b.X, a.Y * b.Y);
    public static LuaVector2 operator *(LuaVector2 a, float b) => new LuaVector2(a.X * b, a.Y * b);
    public static LuaVector2 operator *(float a, LuaVector2 b) => new LuaVector2(a * b.X, a * b.Y);
    public static LuaVector2 operator /(LuaVector2 a, LuaVector2 b) => new LuaVector2(a.X / b.X, a.Y / b.Y);
    public static LuaVector2 operator /(LuaVector2 a, float b) => new LuaVector2(a.X / b, a.Y / b);

    public float Dot(LuaVector2 other) => X * other.X + Y * other.Y;
    public float Cross(LuaVector2 other) => X * other.Y - Y * other.X;
    public float Distance(LuaVector2 other) => (this - other).Magnitude;
    public float Angle(LuaVector2 other)
    {
        var d = (Magnitude * other.Magnitude);
        if (d == 0f) return 0f;
        return Mathf.Acos(Mathf.Clamp(Dot(other) / d, -1f, 1f));
    }
    public LuaVector2 Abs() => new LuaVector2(Mathf.Abs(X), Mathf.Abs(Y));
    public LuaVector2 Floor() => new LuaVector2(Mathf.Floor(X), Mathf.Floor(Y));
    public LuaVector2 Ceil() => new LuaVector2(Mathf.Ceil(X), Mathf.Ceil(Y));
    public LuaVector2 Sign() => new LuaVector2(Mathf.Sign(X), Mathf.Sign(Y));
    public LuaVector2 Min(LuaVector2 other) => new LuaVector2(Mathf.Min(X, other.X), Mathf.Min(Y, other.Y));
    public LuaVector2 Max(LuaVector2 other) => new LuaVector2(Mathf.Max(X, other.X), Mathf.Max(Y, other.Y));
    public bool FuzzyEq(LuaVector2 other, float epsilon) =>
        Mathf.Abs(X - other.X) <= epsilon && Mathf.Abs(Y - other.Y) <= epsilon;

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
    [MoonSharpHidden] public static readonly LuaVector3 XAxis = new LuaVector3(1f, 0f, 0f);
    [MoonSharpHidden] public static readonly LuaVector3 YAxis = new LuaVector3(0f, 1f, 0f);
    [MoonSharpHidden] public static readonly LuaVector3 ZAxis = new LuaVector3(0f, 0f, 1f);

    public float Magnitude => Mathf.Sqrt(X * X + Y * Y + Z * Z);
    public float SquaredMagnitude => X * X + Y * Y + Z * Z;
    public LuaVector3 Unit
    {
        get
        {
            var m = Magnitude;
            return m == 0f ? Zero : new LuaVector3(X / m, Y / m, Z / m);
        }
    }

    public static LuaVector3 operator +(LuaVector3 a, LuaVector3 b) => new LuaVector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static LuaVector3 operator -(LuaVector3 a, LuaVector3 b) => new LuaVector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static LuaVector3 operator -(LuaVector3 a) => new LuaVector3(-a.X, -a.Y, -a.Z);
    public static LuaVector3 operator *(LuaVector3 a, LuaVector3 b) => new LuaVector3(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
    public static LuaVector3 operator *(LuaVector3 a, float b) => new LuaVector3(a.X * b, a.Y * b, a.Z * b);
    public static LuaVector3 operator *(float a, LuaVector3 b) => new LuaVector3(a * b.X, a * b.Y, a * b.Z);
    public static LuaVector3 operator /(LuaVector3 a, LuaVector3 b) => new LuaVector3(a.X / b.X, a.Y / b.Y, a.Z / b.Z);
    public static LuaVector3 operator /(LuaVector3 a, float b) => new LuaVector3(a.X / b, a.Y / b, a.Z / b);

    public float Dot(LuaVector3 other) => X * other.X + Y * other.Y + Z * other.Z;
    public LuaVector3 Cross(LuaVector3 other) => new LuaVector3(
        Y * other.Z - Z * other.Y,
        Z * other.X - X * other.Z,
        X * other.Y - Y * other.X);
    public float Distance(LuaVector3 other) => (this - other).Magnitude;
    public float Angle(LuaVector3 other)
    {
        var d = Magnitude * other.Magnitude;
        if (d == 0f) return 0f;
        return Mathf.Acos(Mathf.Clamp(Dot(other) / d, -1f, 1f));
    }
    public LuaVector3 Abs() => new LuaVector3(Mathf.Abs(X), Mathf.Abs(Y), Mathf.Abs(Z));
    public LuaVector3 Floor() => new LuaVector3(Mathf.Floor(X), Mathf.Floor(Y), Mathf.Floor(Z));
    public LuaVector3 Ceil() => new LuaVector3(Mathf.Ceil(X), Mathf.Ceil(Y), Mathf.Ceil(Z));
    public LuaVector3 Sign() => new LuaVector3(Mathf.Sign(X), Mathf.Sign(Y), Mathf.Sign(Z));
    public LuaVector3 Min(LuaVector3 other) => new LuaVector3(Mathf.Min(X, other.X), Mathf.Min(Y, other.Y), Mathf.Min(Z, other.Z));
    public LuaVector3 Max(LuaVector3 other) => new LuaVector3(Mathf.Max(X, other.X), Mathf.Max(Y, other.Y), Mathf.Max(Z, other.Z));
    public bool FuzzyEq(LuaVector3 other, float epsilon) =>
        Mathf.Abs(X - other.X) <= epsilon && Mathf.Abs(Y - other.Y) <= epsilon && Mathf.Abs(Z - other.Z) <= epsilon;

    public LuaVector3 Lerp(LuaVector3 other, float t) =>
        new LuaVector3(X + (other.X - X) * t, Y + (other.Y - Y) * t, Z + (other.Z - Z) * t);

    public override string ToString() => $"{X}, {Y}, {Z}";

    [MoonSharpHidden]
    public Vector3 ToUnity() => new Vector3(X, Y, Z);
    [MoonSharpHidden]
    public static LuaVector3 FromUnity(Vector3 v) => new LuaVector3(v.x, v.y, v.z);
}

[MoonSharpUserData]
public class LuaColor3
{
    public float R { get; }
    public float G { get; }
    public float B { get; }
    public string ClassName => "Color3";

    public LuaColor3(float r, float g, float b) { R = r; G = g; B = b; }

    public static LuaColor3 operator +(LuaColor3 a, LuaColor3 b) => new LuaColor3(a.R + b.R, a.G + b.G, a.B + b.B);
    public static LuaColor3 operator -(LuaColor3 a, LuaColor3 b) => new LuaColor3(a.R - b.R, a.G - b.G, a.B - b.B);
    public static LuaColor3 operator *(LuaColor3 a, LuaColor3 b) => new LuaColor3(a.R * b.R, a.G * b.G, a.B * b.B);
    public static LuaColor3 operator *(LuaColor3 a, float b) => new LuaColor3(a.R * b, a.G * b, a.B * b);
    public static LuaColor3 operator *(float a, LuaColor3 b) => new LuaColor3(a * b.R, a * b.G, a * b.B);

    public LuaColor3 Lerp(LuaColor3 other, float t) =>
        new LuaColor3(R + (other.R - R) * t, G + (other.G - G) * t, B + (other.B - B) * t);

    public string ToHex()
    {
        int r = Mathf.Clamp(Mathf.RoundToInt(R * 255f), 0, 255);
        int g = Mathf.Clamp(Mathf.RoundToInt(G * 255f), 0, 255);
        int b = Mathf.Clamp(Mathf.RoundToInt(B * 255f), 0, 255);
        return $"#{r:X2}{g:X2}{b:X2}";
    }

    public DynValue ToHSV(Script s)
    {
        Color.RGBToHSV(new Color(R, G, B), out var h, out var sat, out var v);
        var t = new Table(s);
        t["H"] = h; t["S"] = sat; t["V"] = v;
        return DynValue.NewTable(t);
    }

    public override string ToString() => $"{R}, {G}, {B}";
}

[MoonSharpUserData]
public class LuaUDim
{
    public float Scale { get; }
    public float Offset { get; }
    public string ClassName => "UDim";

    public LuaUDim(float scale, float offset) { Scale = scale; Offset = offset; }

    public static LuaUDim operator +(LuaUDim a, LuaUDim b) => new LuaUDim(a.Scale + b.Scale, a.Offset + b.Offset);
    public static LuaUDim operator -(LuaUDim a, LuaUDim b) => new LuaUDim(a.Scale - b.Scale, a.Offset - b.Offset);
    public static LuaUDim operator -(LuaUDim a) => new LuaUDim(-a.Scale, -a.Offset);
    public static LuaUDim operator *(LuaUDim a, float b) => new LuaUDim(a.Scale * b, a.Offset * b);
    public static LuaUDim operator /(LuaUDim a, float b) => new LuaUDim(a.Scale / b, a.Offset / b);

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
        new LuaUDim2(a.X + b.X, a.Y + b.Y);
    public static LuaUDim2 operator -(LuaUDim2 a, LuaUDim2 b) =>
        new LuaUDim2(a.X - b.X, a.Y - b.Y);
    public static LuaUDim2 operator -(LuaUDim2 a) => new LuaUDim2(-a.X, -a.Y);
    public static LuaUDim2 operator *(LuaUDim2 a, float b) => new LuaUDim2(a.X * b, a.Y * b);
    public static LuaUDim2 operator /(LuaUDim2 a, float b) => new LuaUDim2(a.X / b, a.Y / b);

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

    [MoonSharpHidden] public static readonly LuaCFrame Identity =
        new LuaCFrame(LuaVector3.Zero, LuaVector3.Zero);

    public LuaCFrame(LuaVector3 position, LuaVector3 rotation)
    {
        Position = position;
        Rotation = rotation;
    }

    [MoonSharpHidden]
    public Quaternion Quat => Quaternion.Euler(Rotation.X, Rotation.Y, Rotation.Z);

    public LuaVector3 LookVector
    {
        get
        {
            var v = Quat * UnityEngine.Vector3.forward;
            return new LuaVector3(v.x, v.y, v.z);
        }
    }
    public LuaVector3 RightVector
    {
        get
        {
            var v = Quat * UnityEngine.Vector3.right;
            return new LuaVector3(v.x, v.y, v.z);
        }
    }
    public LuaVector3 UpVector
    {
        get
        {
            var v = Quat * UnityEngine.Vector3.up;
            return new LuaVector3(v.x, v.y, v.z);
        }
    }

    public LuaCFrame Lerp(LuaCFrame other, float t) =>
        new LuaCFrame(Position.Lerp(other.Position, t), Rotation.Lerp(other.Rotation, t));

    public LuaCFrame Inverse()
    {
        var inv = Quaternion.Inverse(Quat);
        var p = inv * new UnityEngine.Vector3(-Position.X, -Position.Y, -Position.Z);
        var e = inv.eulerAngles;
        return new LuaCFrame(new LuaVector3(p.x, p.y, p.z), new LuaVector3(e.x, e.y, e.z));
    }

    public LuaCFrame ToWorldSpace(LuaCFrame cf) => this * cf;
    public LuaCFrame ToObjectSpace(LuaCFrame cf) => Inverse() * cf;
    public LuaVector3 PointToWorldSpace(LuaVector3 v) => this * v;
    public LuaVector3 PointToObjectSpace(LuaVector3 v) => Inverse() * v;
    public LuaVector3 VectorToWorldSpace(LuaVector3 v)
    {
        var r = Quat * new UnityEngine.Vector3(v.X, v.Y, v.Z);
        return new LuaVector3(r.x, r.y, r.z);
    }
    public LuaVector3 VectorToObjectSpace(LuaVector3 v)
    {
        var r = Quaternion.Inverse(Quat) * new UnityEngine.Vector3(v.X, v.Y, v.Z);
        return new LuaVector3(r.x, r.y, r.z);
    }

    public static LuaCFrame operator +(LuaCFrame a, LuaVector3 v) =>
        new LuaCFrame(a.Position + v, a.Rotation);
    public static LuaCFrame operator -(LuaCFrame a, LuaVector3 v) =>
        new LuaCFrame(a.Position - v, a.Rotation);

    public static LuaCFrame operator *(LuaCFrame a, LuaCFrame b)
    {
        var qa = a.Quat;
        var qb = b.Quat;
        var rotated = qa * new UnityEngine.Vector3(b.Position.X, b.Position.Y, b.Position.Z);
        var pos = new LuaVector3(
            a.Position.X + rotated.x,
            a.Position.Y + rotated.y,
            a.Position.Z + rotated.z);
        var euler = (qa * qb).eulerAngles;
        return new LuaCFrame(pos, new LuaVector3(euler.x, euler.y, euler.z));
    }

    public static LuaVector3 operator *(LuaCFrame a, LuaVector3 v)
    {
        var rotated = a.Quat * new UnityEngine.Vector3(v.X, v.Y, v.Z);
        return new LuaVector3(
            a.Position.X + rotated.x,
            a.Position.Y + rotated.y,
            a.Position.Z + rotated.z);
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
        t["xAxis"] = LuaVector2.XAxis;
        t["yAxis"] = LuaVector2.YAxis;
        return t;
    }

    private static Table BuildVector3(Script s)
    {
        var t = new Table(s);
        t["new"] = (Func<float, float, float, LuaVector3>)((x, y, z) => new LuaVector3(x, y, z));
        t["zero"] = LuaVector3.Zero;
        t["one"] = LuaVector3.One;
        t["xAxis"] = LuaVector3.XAxis;
        t["yAxis"] = LuaVector3.YAxis;
        t["zAxis"] = LuaVector3.ZAxis;
        return t;
    }

    private static Table BuildColor3(Script s)
    {
        var t = new Table(s);
        t["new"] = (Func<float, float, float, LuaColor3>)((r, g, b) => new LuaColor3(r, g, b));
        t["fromRGB"] = (Func<int, int, int, LuaColor3>)((r, g, b) =>
            new LuaColor3(r / 255f, g / 255f, b / 255f));
        t["fromHSV"] = (Func<float, float, float, LuaColor3>)((h, sat, v) =>
        {
            var c = Color.HSVToRGB(h, sat, v);
            return new LuaColor3(c.r, c.g, c.b);
        });
        t["fromHex"] = (Func<string, LuaColor3>)((hex) =>
        {
            if (string.IsNullOrEmpty(hex)) throw new ScriptRuntimeException("Color3.fromHex: empty hex");
            if (hex[0] == '#') hex = hex.Substring(1);
            if (hex.Length != 6) throw new ScriptRuntimeException("Color3.fromHex: expected 6 hex chars");
            int r = Convert.ToInt32(hex.Substring(0, 2), 16);
            int g = Convert.ToInt32(hex.Substring(2, 2), 16);
            int b = Convert.ToInt32(hex.Substring(4, 2), 16);
            return new LuaColor3(r / 255f, g / 255f, b / 255f);
        });
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
        t["new"] = DynValue.NewCallback((ctx, args) =>
        {
            if (args.Count == 0) return UserData.Create(LuaCFrame.Identity);
            if (args.Count == 3 && args[0].Type == DataType.Number && args[1].Type == DataType.Number && args[2].Type == DataType.Number)
            {
                return UserData.Create(new LuaCFrame(
                    new LuaVector3((float)args[0].Number, (float)args[1].Number, (float)args[2].Number),
                    LuaVector3.Zero));
            }
            if (args.Count == 6)
            {
                return UserData.Create(new LuaCFrame(
                    new LuaVector3((float)args[0].Number, (float)args[1].Number, (float)args[2].Number),
                    new LuaVector3((float)args[3].Number, (float)args[4].Number, (float)args[5].Number)));
            }
            if (args.Count == 1 && args[0].UserData?.Object is LuaVector3 v)
            {
                return UserData.Create(new LuaCFrame(v, LuaVector3.Zero));
            }
            if (args.Count == 2 && args[0].UserData?.Object is LuaVector3 vp && args[1].UserData?.Object is LuaVector3 vr)
            {
                return UserData.Create(new LuaCFrame(vp, vr));
            }
            return UserData.Create(LuaCFrame.Identity);
        });
        t["identity"] = LuaCFrame.Identity;
        t["fromPosition"] = (Func<LuaVector3, LuaCFrame>)((pos) =>
            new LuaCFrame(pos, LuaVector3.Zero));
        t["fromEulerAngles"] = (Func<LuaVector3, LuaVector3, LuaCFrame>)((pos, rot) =>
            new LuaCFrame(pos, rot));
        t["Angles"] = (Func<float, float, float, LuaCFrame>)((rx, ry, rz) =>
            new LuaCFrame(LuaVector3.Zero, new LuaVector3(rx, ry, rz)));
        t["fromAxisAngle"] = (Func<LuaVector3, float, LuaCFrame>)((axis, angleRad) =>
        {
            var q = Quaternion.AngleAxis(angleRad * Mathf.Rad2Deg, new Vector3(axis.X, axis.Y, axis.Z));
            var e = q.eulerAngles;
            return new LuaCFrame(LuaVector3.Zero, new LuaVector3(e.x, e.y, e.z));
        });
        t["LookAt"] = DynValue.NewCallback((ctx, args) =>
        {
            if (args.Count < 2) throw new ScriptRuntimeException("CFrame.LookAt(eye, target [, up])");
            var eye = args[0].UserData?.Object as LuaVector3
                ?? throw new ScriptRuntimeException("CFrame.LookAt: eye must be Vector3");
            var target = args[1].UserData?.Object as LuaVector3
                ?? throw new ScriptRuntimeException("CFrame.LookAt: target must be Vector3");
            var up = (args.Count > 2 ? args[2].UserData?.Object as LuaVector3 : null) ?? LuaVector3.YAxis;
            var fwd = new Vector3(target.X - eye.X, target.Y - eye.Y, target.Z - eye.Z);
            if (fwd.sqrMagnitude < 1e-10f) fwd = Vector3.forward;
            var q = Quaternion.LookRotation(fwd.normalized, new Vector3(up.X, up.Y, up.Z));
            var e = q.eulerAngles;
            return UserData.Create(new LuaCFrame(eye, new LuaVector3(e.x, e.y, e.z)));
        });
        return t;
    }
}

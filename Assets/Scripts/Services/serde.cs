using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

public class SerdeService : LuaService
{
    public override void Register(Script script)
    {
        var serde = new Table(script);

        serde["encode"] = DynValue.NewCallback((ctx, args) =>
        {
            var format = args.Count > 0 ? args[0].String : null;
            var data = args.Count > 1 ? args[1] : DynValue.Nil;
            var pretty = args.Count > 2 && args[2].CastToBool();
            return DynValue.NewString(Encode(format, data, pretty));
        });

        serde["decode"] = DynValue.NewCallback((ctx, args) =>
        {
            var format = args.Count > 0 ? args[0].String : null;
            var raw = args.Count > 1 ? args[1].String : null;
            return Decode(script, format, raw);
        });

        serde["hash"] = (Func<string, string, string>)Hash;

        script.Globals["Serde"] = serde;
    }

    private static string Encode(string format, DynValue data, bool pretty)
    {
        switch ((format ?? "").ToLower())
        {
            case "json":
                var sb = new StringBuilder();
                WriteJson(data, sb, pretty, 0);
                return sb.ToString();
            default:
                throw new ScriptRuntimeException($"Serde.encode: unsupported format \"{format}\"");
        }
    }

    private static DynValue Decode(Script script, string format, string raw)
    {
        if (raw == null) return DynValue.Nil;
        switch ((format ?? "").ToLower())
        {
            case "json":
                int idx = 0;
                SkipWs(raw, ref idx);
                var v = ReadJson(script, raw, ref idx);
                return v;
            default:
                throw new ScriptRuntimeException($"Serde.decode: unsupported format \"{format}\"");
        }
    }

    private static string Hash(string algo, string input)
    {
        if (input == null) input = "";
        var bytes = Encoding.UTF8.GetBytes(input);
        byte[] result;
        switch ((algo ?? "").ToLower())
        {
            case "md5":
                using (var h = MD5.Create()) result = h.ComputeHash(bytes);
                break;
            case "sha1":
                using (var h = SHA1.Create()) result = h.ComputeHash(bytes);
                break;
            case "sha256":
                using (var h = SHA256.Create()) result = h.ComputeHash(bytes);
                break;
            case "sha384":
                using (var h = SHA384.Create()) result = h.ComputeHash(bytes);
                break;
            case "sha512":
                using (var h = SHA512.Create()) result = h.ComputeHash(bytes);
                break;
            case "crc32":
                return Crc32(bytes).ToString("x8");
            default:
                throw new ScriptRuntimeException($"Serde.hash: unsupported algorithm \"{algo}\"");
        }
        var sb = new StringBuilder(result.Length * 2);
        for (int i = 0; i < result.Length; i++) sb.Append(result[i].ToString("x2"));
        return sb.ToString();
    }

    private static uint Crc32(byte[] bytes)
    {
        uint crc = 0xFFFFFFFFu;
        for (int i = 0; i < bytes.Length; i++)
        {
            crc ^= bytes[i];
            for (int j = 0; j < 8; j++)
                crc = (crc >> 1) ^ ((crc & 1u) != 0 ? 0xEDB88320u : 0u);
        }
        return ~crc;
    }

    // ---------------- JSON encode ----------------

    private static void WriteJson(DynValue v, StringBuilder sb, bool pretty, int depth)
    {
        switch (v.Type)
        {
            case DataType.Nil:
            case DataType.Void:
                sb.Append("null"); break;
            case DataType.Boolean:
                sb.Append(v.Boolean ? "true" : "false"); break;
            case DataType.Number:
                sb.Append(v.Number.ToString("R", CultureInfo.InvariantCulture)); break;
            case DataType.String:
                WriteJsonString(v.String, sb); break;
            case DataType.Table:
                WriteJsonTable(v.Table, sb, pretty, depth); break;
            default:
                sb.Append("null"); break;
        }
    }

    private static void WriteJsonTable(Table t, StringBuilder sb, bool pretty, int depth)
    {
        bool isArray = IsArray(t);
        string nl = pretty ? "\n" : "";
        string indent = pretty ? new string(' ', (depth + 1) * 2) : "";
        string closeIndent = pretty ? new string(' ', depth * 2) : "";
        string sep = pretty ? ": " : ":";

        if (isArray)
        {
            sb.Append('[');
            int len = t.Length;
            if (len == 0) { sb.Append(']'); return; }
            sb.Append(nl);
            for (int i = 1; i <= len; i++)
            {
                sb.Append(indent);
                WriteJson(t.Get(i), sb, pretty, depth + 1);
                if (i < len) sb.Append(',');
                sb.Append(nl);
            }
            sb.Append(closeIndent).Append(']');
        }
        else
        {
            sb.Append('{');
            var pairs = new List<TablePair>();
            foreach (var pair in t.Pairs) pairs.Add(pair);
            if (pairs.Count == 0) { sb.Append('}'); return; }
            sb.Append(nl);
            for (int i = 0; i < pairs.Count; i++)
            {
                var p = pairs[i];
                sb.Append(indent);
                WriteJsonString(p.Key.ToPrintString(), sb);
                sb.Append(sep);
                WriteJson(p.Value, sb, pretty, depth + 1);
                if (i < pairs.Count - 1) sb.Append(',');
                sb.Append(nl);
            }
            sb.Append(closeIndent).Append('}');
        }
    }

    private static bool IsArray(Table t)
    {
        int len = t.Length;
        if (len == 0)
        {
            foreach (var _ in t.Pairs) return false;
            return true;
        }
        int count = 0;
        foreach (var _ in t.Pairs) count++;
        return count == len;
    }

    private static void WriteJsonString(string s, StringBuilder sb)
    {
        sb.Append('"');
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                    else sb.Append(c);
                    break;
            }
        }
        sb.Append('"');
    }

    // ---------------- JSON decode ----------------

    private static void SkipWs(string s, ref int i)
    {
        while (i < s.Length && (s[i] == ' ' || s[i] == '\t' || s[i] == '\n' || s[i] == '\r')) i++;
    }

    private static DynValue ReadJson(Script script, string s, ref int i)
    {
        SkipWs(s, ref i);
        if (i >= s.Length) throw new ScriptRuntimeException("Serde.decode: unexpected end");
        char c = s[i];
        if (c == '{') return ReadObject(script, s, ref i);
        if (c == '[') return ReadArray(script, s, ref i);
        if (c == '"') return DynValue.NewString(ReadString(s, ref i));
        if (c == 't' || c == 'f') return ReadBool(s, ref i);
        if (c == 'n') return ReadNull(s, ref i);
        return ReadNumber(s, ref i);
    }

    private static DynValue ReadObject(Script script, string s, ref int i)
    {
        i++;
        var t = new Table(script);
        SkipWs(s, ref i);
        if (i < s.Length && s[i] == '}') { i++; return DynValue.NewTable(t); }
        while (true)
        {
            SkipWs(s, ref i);
            var key = ReadString(s, ref i);
            SkipWs(s, ref i);
            if (i >= s.Length || s[i] != ':') throw new ScriptRuntimeException("Serde.decode: expected ':'");
            i++;
            var val = ReadJson(script, s, ref i);
            t[key] = val;
            SkipWs(s, ref i);
            if (i < s.Length && s[i] == ',') { i++; continue; }
            if (i < s.Length && s[i] == '}') { i++; return DynValue.NewTable(t); }
            throw new ScriptRuntimeException("Serde.decode: expected ',' or '}'");
        }
    }

    private static DynValue ReadArray(Script script, string s, ref int i)
    {
        i++;
        var t = new Table(script);
        SkipWs(s, ref i);
        if (i < s.Length && s[i] == ']') { i++; return DynValue.NewTable(t); }
        int idx = 1;
        while (true)
        {
            var val = ReadJson(script, s, ref i);
            t[idx++] = val;
            SkipWs(s, ref i);
            if (i < s.Length && s[i] == ',') { i++; continue; }
            if (i < s.Length && s[i] == ']') { i++; return DynValue.NewTable(t); }
            throw new ScriptRuntimeException("Serde.decode: expected ',' or ']'");
        }
    }

    private static string ReadString(string s, ref int i)
    {
        if (s[i] != '"') throw new ScriptRuntimeException("Serde.decode: expected string");
        i++;
        var sb = new StringBuilder();
        while (i < s.Length)
        {
            char c = s[i++];
            if (c == '"') return sb.ToString();
            if (c == '\\')
            {
                if (i >= s.Length) break;
                char e = s[i++];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (i + 4 > s.Length) throw new ScriptRuntimeException("Serde.decode: bad \\u");
                        sb.Append((char)int.Parse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                        i += 4; break;
                    default: sb.Append(e); break;
                }
            }
            else sb.Append(c);
        }
        throw new ScriptRuntimeException("Serde.decode: unterminated string");
    }

    private static DynValue ReadBool(string s, ref int i)
    {
        if (i + 4 <= s.Length && s.Substring(i, 4) == "true") { i += 4; return DynValue.True; }
        if (i + 5 <= s.Length && s.Substring(i, 5) == "false") { i += 5; return DynValue.False; }
        throw new ScriptRuntimeException("Serde.decode: bad bool");
    }

    private static DynValue ReadNull(string s, ref int i)
    {
        if (i + 4 <= s.Length && s.Substring(i, 4) == "null") { i += 4; return DynValue.Nil; }
        throw new ScriptRuntimeException("Serde.decode: bad null");
    }

    private static DynValue ReadNumber(string s, ref int i)
    {
        int start = i;
        if (s[i] == '-') i++;
        while (i < s.Length && ((s[i] >= '0' && s[i] <= '9') || s[i] == '.' || s[i] == 'e' || s[i] == 'E' || s[i] == '+' || s[i] == '-')) i++;
        var n = double.Parse(s.Substring(start, i - start), CultureInfo.InvariantCulture);
        return DynValue.NewNumber(n);
    }
}

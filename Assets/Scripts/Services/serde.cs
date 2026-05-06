using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
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

        serde["compress"] = DynValue.NewCallback((ctx, args) =>
        {
            var algo = args.Count > 0 ? args[0].String : "gzip";
            var raw = args.Count > 1 ? args[1].String : null;
            return DynValue.NewString(Compress(algo, raw));
        });

        serde["decompress"] = DynValue.NewCallback((ctx, args) =>
        {
            var algo = args.Count > 0 ? args[0].String : "gzip";
            var raw = args.Count > 1 ? args[1].String : null;
            return DynValue.NewString(Decompress(algo, raw));
        });

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
            case "toml":
                if (data.Type != DataType.Table)
                    throw new ScriptRuntimeException("Serde.encode toml: expected a table");
                var tomlSb = new StringBuilder();
                WriteToml(data.Table, tomlSb, "");
                return tomlSb.ToString();
            case "yaml":
            case "yml":
                var yamlSb = new StringBuilder();
                WriteYaml(data, yamlSb, 0, false);
                return yamlSb.ToString();
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
            case "toml":
                return ReadToml(script, raw);
            case "yaml":
            case "yml":
                return ReadYaml(script, raw);
            default:
                throw new ScriptRuntimeException($"Serde.decode: unsupported format \"{format}\"");
        }
    }

    private static string Compress(string algo, string input)
    {
        if (input == null) return "";
        var bytes = Encoding.UTF8.GetBytes(input);
        using var ms = new MemoryStream();
        switch ((algo ?? "").ToLower())
        {
            case "gzip":
                using (var z = new GZipStream(ms, CompressionLevel.Optimal, leaveOpen: true))
                    z.Write(bytes, 0, bytes.Length);
                break;
            case "deflate":
                using (var z = new DeflateStream(ms, CompressionLevel.Optimal, leaveOpen: true))
                    z.Write(bytes, 0, bytes.Length);
                break;
            default:
                throw new ScriptRuntimeException($"Serde.compress: unsupported algorithm \"{algo}\"");
        }
        return Convert.ToBase64String(ms.ToArray());
    }

    private static string Decompress(string algo, string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        var bytes = Convert.FromBase64String(input);
        using var src = new MemoryStream(bytes);
        using var dest = new MemoryStream();
        switch ((algo ?? "").ToLower())
        {
            case "gzip":
                using (var z = new GZipStream(src, CompressionMode.Decompress)) z.CopyTo(dest);
                break;
            case "deflate":
                using (var z = new DeflateStream(src, CompressionMode.Decompress)) z.CopyTo(dest);
                break;
            default:
                throw new ScriptRuntimeException($"Serde.decompress: unsupported algorithm \"{algo}\"");
        }
        return Encoding.UTF8.GetString(dest.ToArray());
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

    private static void WriteToml(Table t, StringBuilder sb, string prefix)
    {

        foreach (var p in t.Pairs)
        {
            var key = p.Key.ToPrintString();
            var val = p.Value;
            if (val.Type == DataType.Table && !IsArray(val.Table)) continue;
            sb.Append(EscapeTomlKey(key)).Append(" = ");
            WriteTomlValue(val, sb);
            sb.Append('\n');
        }

        foreach (var p in t.Pairs)
        {
            var val = p.Value;
            if (val.Type != DataType.Table || IsArray(val.Table)) continue;
            var key = p.Key.ToPrintString();
            var section = string.IsNullOrEmpty(prefix) ? key : prefix + "." + key;
            sb.Append('\n').Append('[').Append(section).Append("]\n");
            WriteToml(val.Table, sb, section);
        }
    }

    private static string EscapeTomlKey(string k)
    {
        for (int i = 0; i < k.Length; i++)
        {
            char c = k[i];
            bool ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_' || c == '-';
            if (!ok)
            {
                var sb = new StringBuilder();
                sb.Append('"');
                for (int j = 0; j < k.Length; j++) { if (k[j] == '"' || k[j] == '\\') sb.Append('\\'); sb.Append(k[j]); }
                sb.Append('"');
                return sb.ToString();
            }
        }
        return k;
    }

    private static void WriteTomlValue(DynValue v, StringBuilder sb)
    {
        switch (v.Type)
        {
            case DataType.Nil: case DataType.Void:
                sb.Append("\"\""); break;
            case DataType.Boolean:
                sb.Append(v.Boolean ? "true" : "false"); break;
            case DataType.Number:
                sb.Append(v.Number.ToString("R", CultureInfo.InvariantCulture)); break;
            case DataType.String:
                sb.Append('"');
                foreach (var c in v.String)
                {
                    if (c == '"' || c == '\\') sb.Append('\\');
                    sb.Append(c);
                }
                sb.Append('"');
                break;
            case DataType.Table:
                sb.Append('[');
                int n = v.Table.Length;
                for (int i = 1; i <= n; i++)
                {
                    if (i > 1) sb.Append(", ");
                    WriteTomlValue(v.Table.Get(i), sb);
                }
                sb.Append(']');
                break;
            default:
                sb.Append("\"\""); break;
        }
    }

    private static DynValue ReadToml(Script script, string s)
    {
        var root = new Table(script);
        Table current = root;
        var lines = s.Replace("\r\n", "\n").Split('\n');
        for (int li = 0; li < lines.Length; li++)
        {
            var line = lines[li].Trim();
            if (line.Length == 0 || line[0] == '#') continue;
            if (line[0] == '[')
            {
                int close = line.IndexOf(']');
                if (close < 0) throw new ScriptRuntimeException("Serde.decode toml: bad section header");
                var path = line.Substring(1, close - 1).Trim();
                current = ResolveTomlSection(root, path, script);
                continue;
            }
            int eq = line.IndexOf('=');
            if (eq < 0) continue;
            var key = line.Substring(0, eq).Trim();
            if (key.Length >= 2 && key[0] == '"' && key[key.Length - 1] == '"')
                key = key.Substring(1, key.Length - 2);
            var rawVal = line.Substring(eq + 1).Trim();
            int hash = FindTomlComment(rawVal);
            if (hash >= 0) rawVal = rawVal.Substring(0, hash).Trim();
            current[key] = ParseTomlScalar(rawVal, script);
        }
        return DynValue.NewTable(root);
    }

    private static int FindTomlComment(string v)
    {
        bool inStr = false;
        for (int i = 0; i < v.Length; i++)
        {
            char c = v[i];
            if (c == '"') inStr = !inStr;
            else if (c == '#' && !inStr) return i;
        }
        return -1;
    }

    private static Table ResolveTomlSection(Table root, string path, Script script)
    {
        var parts = path.Split('.');
        Table cur = root;
        foreach (var raw in parts)
        {
            var part = raw.Trim();
            if (part.Length >= 2 && part[0] == '"' && part[part.Length - 1] == '"')
                part = part.Substring(1, part.Length - 2);
            var existing = cur.Get(part);
            if (existing.Type == DataType.Table)
                cur = existing.Table;
            else
            {
                var t = new Table(script);
                cur[part] = DynValue.NewTable(t);
                cur = t;
            }
        }
        return cur;
    }

    private static DynValue ParseTomlScalar(string v, Script script)
    {
        if (v.Length == 0) return DynValue.Nil;
        if (v == "true") return DynValue.True;
        if (v == "false") return DynValue.False;
        if (v[0] == '"' && v.Length >= 2 && v[v.Length - 1] == '"')
        {
            var inner = v.Substring(1, v.Length - 2);
            return DynValue.NewString(inner.Replace("\\\"", "\"").Replace("\\\\", "\\"));
        }
        if (v[0] == '[')
        {
            var t = new Table(script);
            var inner = v.Trim();
            inner = inner.Substring(1, inner.Length - 2);
            int idx = 1;
            foreach (var part in SplitTopLevelCommas(inner))
            {
                var el = part.Trim();
                if (el.Length == 0) continue;
                t[idx++] = ParseTomlScalar(el, script);
            }
            return DynValue.NewTable(t);
        }
        if (double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var n))
            return DynValue.NewNumber(n);
        return DynValue.NewString(v);
    }

    private static IEnumerable<string> SplitTopLevelCommas(string s)
    {
        int depth = 0; bool inStr = false; int start = 0;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c == '"') inStr = !inStr;
            else if (!inStr && c == '[') depth++;
            else if (!inStr && c == ']') depth--;
            else if (!inStr && depth == 0 && c == ',')
            {
                yield return s.Substring(start, i - start);
                start = i + 1;
            }
        }
        if (start < s.Length) yield return s.Substring(start);
    }

    private static void WriteYaml(DynValue v, StringBuilder sb, int depth, bool inSeq)
    {
        var indent = new string(' ', depth * 2);
        switch (v.Type)
        {
            case DataType.Nil: case DataType.Void:
                sb.Append("null"); break;
            case DataType.Boolean:
                sb.Append(v.Boolean ? "true" : "false"); break;
            case DataType.Number:
                sb.Append(v.Number.ToString("R", CultureInfo.InvariantCulture)); break;
            case DataType.String:
                sb.Append(YamlString(v.String)); break;
            case DataType.Table:
                if (IsArray(v.Table))
                {
                    int n = v.Table.Length;
                    if (n == 0) { sb.Append("[]"); break; }
                    for (int i = 1; i <= n; i++)
                    {
                        if (i > 1 || inSeq) sb.Append('\n');
                        sb.Append(indent).Append("- ");
                        var el = v.Table.Get(i);
                        if (el.Type == DataType.Table) { sb.Append('\n'); WriteYaml(el, sb, depth + 1, false); }
                        else WriteYaml(el, sb, depth + 1, true);
                    }
                }
                else
                {
                    bool first = true;
                    foreach (var p in v.Table.Pairs)
                    {
                        if (!first) sb.Append('\n');
                        first = false;
                        sb.Append(indent).Append(YamlKey(p.Key.ToPrintString())).Append(':');
                        if (p.Value.Type == DataType.Table)
                        {
                            sb.Append('\n');
                            WriteYaml(p.Value, sb, depth + 1, false);
                        }
                        else
                        {
                            sb.Append(' ');
                            WriteYaml(p.Value, sb, depth + 1, false);
                        }
                    }
                }
                break;
        }
    }

    private static string YamlKey(string k)
    {
        foreach (var c in k)
        {
            bool ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_' || c == '-';
            if (!ok) return YamlString(k);
        }
        return k;
    }

    private static string YamlString(string s)
    {
        bool needsQuote = s.Length == 0 || s.Contains(":") || s.Contains("#") || s.Contains("\n") || s == "true" || s == "false" || s == "null";
        if (!needsQuote) return s;
        var sb = new StringBuilder();
        sb.Append('"');
        foreach (var c in s)
        {
            if (c == '"' || c == '\\') sb.Append('\\');
            if (c == '\n') { sb.Append("\\n"); continue; }
            sb.Append(c);
        }
        sb.Append('"');
        return sb.ToString();
    }

    private static DynValue ReadYaml(Script script, string source)
    {
        var rawLines = source.Replace("\r\n", "\n").Split('\n');
        var lines = new List<(int indent, string text)>();
        foreach (var raw in rawLines)
        {
            int i = 0;
            while (i < raw.Length && raw[i] == ' ') i++;
            if (i >= raw.Length) continue;
            if (raw[i] == '#') continue;
            lines.Add((i, raw.Substring(i)));
        }
        int idx = 0;
        return ReadYamlBlock(script, lines, ref idx, 0);
    }

    private static DynValue ReadYamlBlock(Script script, List<(int indent, string text)> lines, ref int idx, int minIndent)
    {
        if (idx >= lines.Count) return DynValue.Nil;
        var (firstIndent, firstText) = lines[idx];
        if (firstIndent < minIndent) return DynValue.Nil;

        if (firstText.StartsWith("- "))
        {
            var arr = new Table(script);
            int n = 1;
            while (idx < lines.Count && lines[idx].indent == firstIndent && lines[idx].text.StartsWith("- "))
            {
                var rest = lines[idx].text.Substring(2);
                idx++;
                if (rest.Contains(":") && !rest.StartsWith("\""))
                {

                    var inlineLines = new List<(int, string)>();
                    inlineLines.Add((firstIndent + 2, rest));
                    while (idx < lines.Count && lines[idx].indent > firstIndent)
                    {
                        inlineLines.Add(lines[idx]);
                        idx++;
                    }
                    int sub = 0;
                    arr[n++] = ReadYamlBlock(script, inlineLines, ref sub, firstIndent + 2);
                }
                else if (rest.Length == 0)
                {
                    arr[n++] = ReadYamlBlock(script, lines, ref idx, firstIndent + 2);
                }
                else
                {
                    arr[n++] = ParseYamlScalar(rest, script);
                }
            }
            return DynValue.NewTable(arr);
        }

        var map = new Table(script);
        while (idx < lines.Count)
        {
            var (curIndent, curText) = lines[idx];
            if (curIndent < firstIndent) break;
            if (curIndent > firstIndent) { idx++; continue; }

            int colon = FindYamlColon(curText);
            if (colon < 0) { idx++; continue; }
            var key = curText.Substring(0, colon).Trim();
            if (key.Length >= 2 && key[0] == '"' && key[key.Length - 1] == '"')
                key = key.Substring(1, key.Length - 2);
            var rest = curText.Substring(colon + 1).Trim();
            idx++;
            if (rest.Length == 0)
            {
                if (idx < lines.Count && lines[idx].indent > curIndent)
                    map[key] = ReadYamlBlock(script, lines, ref idx, curIndent + 1);
                else
                    map[key] = DynValue.Nil;
            }
            else
            {
                map[key] = ParseYamlScalar(rest, script);
            }
        }
        return DynValue.NewTable(map);
    }

    private static int FindYamlColon(string s)
    {
        bool inStr = false;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c == '"') inStr = !inStr;
            else if (!inStr && c == ':') return i;
        }
        return -1;
    }

    private static DynValue ParseYamlScalar(string v, Script script)
    {
        v = v.Trim();
        if (v.Length == 0 || v == "~" || v == "null") return DynValue.Nil;
        if (v == "true") return DynValue.True;
        if (v == "false") return DynValue.False;
        if (v.Length >= 2 && v[0] == '"' && v[v.Length - 1] == '"')
        {
            return DynValue.NewString(v.Substring(1, v.Length - 2)
                .Replace("\\\"", "\"").Replace("\\\\", "\\").Replace("\\n", "\n"));
        }
        if (v[0] == '[' && v[v.Length - 1] == ']')
        {
            var t = new Table(script);
            var inner = v.Substring(1, v.Length - 2);
            int idx = 1;
            foreach (var part in SplitTopLevelCommas(inner))
            {
                var el = part.Trim();
                if (el.Length == 0) continue;
                t[idx++] = ParseYamlScalar(el, script);
            }
            return DynValue.NewTable(t);
        }
        if (double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var n))
            return DynValue.NewNumber(n);
        return DynValue.NewString(v);
    }
}

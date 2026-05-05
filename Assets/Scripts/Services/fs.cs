using MoonSharp.Interpreter;
using System;
using System.IO;
using UnityEngine;

public class FsService : LuaService
{
    private static string Root(string path)
    {
        return Path.Combine(Application.persistentDataPath, path);
    }

    public override void Register(Script script)
    {
        var fs = new Table(script);

        fs["write"] = (Action<string, string>)((path, content) =>
        {
            var full = Root(path);
            var dir = Path.GetDirectoryName(full);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(full, content);
        });

        fs["append"] = (Action<string, string>)((path, content) =>
        {
            var full = Root(path);
            var dir = Path.GetDirectoryName(full);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.AppendAllText(full, content);
        });

        fs["read"] = (Func<string, string>)((path) =>
        {
            var full = Root(path);
            return File.Exists(full) ? File.ReadAllText(full) : null;
        });

        fs["exists"] = (Func<string, bool>)((path) => File.Exists(Root(path)));
        fs["isFile"] = (Func<string, bool>)((path) => File.Exists(Root(path)));
        fs["isDir"] = (Func<string, bool>)((path) => Directory.Exists(Root(path)));

        fs["mkdir"] = (Action<string>)((path) => Directory.CreateDirectory(Root(path)));

        fs["remove"] = (Action<string>)((path) =>
        {
            var full = Root(path);
            if (File.Exists(full)) File.Delete(full);
            else if (Directory.Exists(full)) Directory.Delete(full, true);
        });

        fs["move"] = (Action<string, string>)((from, to) =>
        {
            var src = Root(from); var dst = Root(to);
            var dir = Path.GetDirectoryName(dst);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            if (File.Exists(src)) File.Move(src, dst);
            else if (Directory.Exists(src)) Directory.Move(src, dst);
        });

        fs["copy"] = (Action<string, string>)((from, to) =>
        {
            var src = Root(from); var dst = Root(to);
            var dir = Path.GetDirectoryName(dst);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.Copy(src, dst, true);
        });

        fs["list"] = DynValue.NewCallback((ctx, args) =>
        {
            var path = args.Count > 0 ? args[0].String : "";
            var full = Root(path ?? "");
            var t = new Table(script);
            if (!Directory.Exists(full)) return DynValue.NewTable(t);
            int i = 1;
            foreach (var entry in Directory.GetFileSystemEntries(full))
                t[i++] = Path.GetFileName(entry);
            return DynValue.NewTable(t);
        });

        fs["size"] = (Func<string, long>)((path) =>
        {
            var full = Root(path);
            return File.Exists(full) ? new FileInfo(full).Length : 0L;
        });

        fs["join"] = DynValue.NewCallback((ctx, args) =>
        {
            string acc = "";

            for (int i = 0; i < args.Count; i++)
            {
                var part = args[i].CastToString();
                if (string.IsNullOrEmpty(part)) continue;

                acc = string.IsNullOrEmpty(acc)
                    ? part
                    : acc + "/" + part;
            }

            return DynValue.NewString(acc);
        });

        script.Globals["fs"] = fs;
    }
}

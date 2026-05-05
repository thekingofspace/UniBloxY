using MoonSharp.Interpreter;
using System;
using UnityEngine;

public class SystemService : LuaService
{
    public override void Register(Script script)
    {
        var sys = new Table(script);

        sys["clock"] = (Func<double>)(() => Time.realtimeSinceStartupAsDouble);
        sys["time"] = (Func<long>)(() => DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        sys["timeMillis"] = (Func<long>)(() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        sys["date"] = (Func<string, string>)((format) =>
        {
            var dt = DateTime.Now;
            if (string.IsNullOrEmpty(format) || format == "*t")
                return dt.ToString("yyyy-MM-dd HH:mm:ss");
            return dt.ToString(format);
        });

        sys["utcDate"] = (Func<string, string>)((format) =>
        {
            var dt = DateTime.UtcNow;
            if (string.IsNullOrEmpty(format) || format == "*t")
                return dt.ToString("yyyy-MM-dd HH:mm:ss");
            return dt.ToString(format);
        });

        sys["getenv"] = (Func<string, string>)((key) => Environment.GetEnvironmentVariable(key));

        sys["platform"] = (Func<string>)(() => Application.platform.ToString());
        sys["os"] = (Func<string>)(() => SystemInfo.operatingSystem);
        sys["deviceName"] = (Func<string>)(() => SystemInfo.deviceName);
        sys["deviceModel"] = (Func<string>)(() => SystemInfo.deviceModel);
        sys["processorCount"] = (Func<int>)(() => SystemInfo.processorCount);
        sys["systemMemoryMB"] = (Func<int>)(() => SystemInfo.systemMemorySize);

        sys["documentsPath"] = (Func<string>)(() =>
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        sys["persistentPath"] = (Func<string>)(() => Application.persistentDataPath);
        sys["tempPath"] = (Func<string>)(() => Application.temporaryCachePath);

        sys["frameCount"] = (Func<int>)(() => Time.frameCount);
        sys["deltaTime"] = (Func<float>)(() => Time.deltaTime);

        script.Globals["System"] = sys;
    }
}

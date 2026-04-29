using UnityEditor;
using UnityEditor.AssetImporters;
using System.IO;

[ScriptedImporter(1, "lua")]
public class LuaImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        var text = File.ReadAllText(ctx.assetPath);
        var textAsset = new UnityEngine.TextAsset(text);
        ctx.AddObjectToAsset("main", textAsset);
        ctx.SetMainObject(textAsset);
    }
}
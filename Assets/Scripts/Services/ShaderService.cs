using System;
using MoonSharp.Interpreter;

public class ShaderService : LuaService
{
    public override void Register(Script script)
    {
        lua = script;

        var table = new Table(script);
        table["GetShader"]   = (Func<string, LuaShader>)(name => AssetService.Instance.GetShader(name));
        table["Get"]         = (Func<string, LuaShader>)(name => AssetService.Instance.GetShader(name));
        table["GetMaterial"] = (Func<string, LuaMaterial>)(name => AssetService.Instance.GetMaterial(name));
        table["GetTexture"]  = (Func<string, LuaTexture>)(name => AssetService.Instance.GetTexture(name));
        table["CreateMaterial"] = DynValue.NewCallback((ctx, args) =>
        {
            if (args.Count < 1 || args[0].Type != DataType.String)
                throw new ScriptRuntimeException("ShaderService.CreateMaterial(shaderName [, name])");
            var shaderName = args[0].String;
            var matName = args.Count > 1 && args[1].Type == DataType.String ? args[1].String : null;
            return UserData.Create(AssetService.Instance.CreateMaterial(shaderName, matName));
        });

        script.Globals["ShaderService"] = table;
    }
}

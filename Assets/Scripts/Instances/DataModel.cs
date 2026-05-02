public class DataModel : LuaInstanceClass
{
    public override string ClassName => "DataModel";

    public override void Initialize(LuaInstance instance)
    {
        instance.Indestructible = true;
    }
}

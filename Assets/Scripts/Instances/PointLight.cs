using UnityEngine;

public class PointLight : LightElement
{
    public override string ClassName => "PointLight";

    protected override void ConfigureType(Light light)
    {
        light.type = LightType.Point;
    }
}

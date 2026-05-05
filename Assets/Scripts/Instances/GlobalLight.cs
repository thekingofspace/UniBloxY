using UnityEngine;

public class GlobalLight : LightElement
{
    public override string ClassName => "GlobalLight";

    protected override void ConfigureType(Light light)
    {
        light.type = LightType.Directional;
    }
}

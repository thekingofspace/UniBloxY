return function()
    if type(Lighting) ~= "table" then
        error("Lighting global missing")
    end

    -- Volume properties: round-trip a few values to verify the meta __index/__setter wiring.
    Lighting.FogEnabled = true
    if Lighting.FogEnabled ~= true then error("FogEnabled write/read failed") end

    Lighting.FogStart = 25
    Lighting.FogEnd = 250
    if Lighting.FogStart ~= 25 then error("FogStart round-trip failed") end
    if Lighting.FogEnd ~= 250 then error("FogEnd round-trip failed") end

    -- Numeric round-trips below use values that are exactly representable as
    -- both float (storage) and double (Lua) so we don't chase precision noise.
    Lighting.Exposure = 1.5
    if Lighting.Exposure ~= 1.5 then error("Exposure round-trip failed") end

    Lighting.BloomThreshold = 0.5
    Lighting.BloomIntensity = 0.25
    Lighting.VignetteIntensity = 0.125
    Lighting.Saturation = 1.5
    Lighting.Contrast = 1.25
    if Lighting.BloomThreshold ~= 0.5 then error("BloomThreshold round-trip failed") end
    if Lighting.Contrast ~= 1.25 then error("Contrast round-trip failed") end

    local amb = Color3.new(0.2, 0.3, 0.4)
    Lighting.Ambient = amb
    if typeof(Lighting.Ambient) ~= "Color3" then error("Ambient must be a Color3") end
    if Lighting.Ambient ~= amb then error("Ambient round-trip failed (equality by RGB)") end

    -- GetPostProcessing returns a PostProcessing override bag.
    local pp = Lighting:GetPostProcessing()
    if typeof(pp) ~= "PostProcessing" then error("GetPostProcessing must return a PostProcessing") end
    pp:Set("Bloom", 1.5)
    if pp:Has("Bloom") ~= true then error("PostProcessing:Has failed") end
    if pp:Get("Bloom") ~= 1.5 then error("PostProcessing:Get failed") end
    pp:Set("Bloom", nil)
    if pp:Has("Bloom") ~= false then error("PostProcessing:Set(nil) should remove key") end

    -- RaycastParams.new + Transformer round-trip.
    local rp = RaycastParams.new()
    if typeof(rp) ~= "RaycastParams" then error("RaycastParams.new must return a RaycastParams") end
    rp.MaxDistance = 50
    if rp.MaxDistance ~= 50 then error("RaycastParams.MaxDistance round-trip failed") end

    local transformerSeen = false
    rp.Transformer = function(_) transformerSeen = true; return true end

    -- A ray into empty space returns nil. We don't assert on hits because the
    -- test scene has no colliders — but the call must not error.
    local result = Lighting:Raycast(Vector3.new(0, 1000, 0), Vector3.new(0, 1001, 0), rp)
    if result ~= nil and typeof(result) ~= "RaycastResult" then
        error("Raycast must return nil or a RaycastResult")
    end

    -- Hold the volume settings (fog, ambient, exposure) so they're visible.
    return 3
end

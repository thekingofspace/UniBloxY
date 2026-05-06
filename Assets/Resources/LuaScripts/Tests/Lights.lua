return function()
    -- Construct each light type and verify the shared LightElement properties
    -- are reachable + writable, plus the type-specific extras.

    local function checkBase(light, label)
        if typeof(light.Color) ~= "Color3" then
            error(label .. ".Color must default to a Color3")
        end
        if type(light.Intensity)  ~= "number"  then error(label .. ".Intensity missing") end
        if type(light.Range)      ~= "number"  then error(label .. ".Range missing") end
        if type(light.Brightness) ~= "number"  then error(label .. ".Brightness missing") end
        if type(light.ShadowType) ~= "string"  then error(label .. ".ShadowType missing") end
        if type(light.Active)     ~= "boolean" then error(label .. ".Active missing") end
        if type(light.RealTime)   ~= "boolean" then error(label .. ".RealTime missing") end

        light.Color = Color3.new(1, 0, 0)
        if light.Color ~= Color3.new(1, 0, 0) then error(label .. ".Color round-trip failed") end

        -- All numeric round-trips below use values that are exactly
        -- representable as both float (storage) and double (Lua) so we don't
        -- chase precision noise.
        light.Intensity = 2.5
        if light.Intensity ~= 2.5 then error(label .. ".Intensity round-trip failed") end

        light.Range = 25
        if light.Range ~= 25 then error(label .. ".Range round-trip failed") end

        light.Brightness = 0.75
        if light.Brightness ~= 0.75 then error(label .. ".Brightness round-trip failed") end

        light.ShadowType = "Realistic"
        if light.ShadowType ~= "Realistic" then error(label .. ".ShadowType round-trip failed") end

        light.Active = false
        if light.Active ~= false then error(label .. ".Active round-trip failed") end
        light.Active = true

        light.RealTime = true
        light.NearPlane = 0.5
        light.Strength = 0.5
        if light.NearPlane ~= 0.5 then error(label .. ".NearPlane round-trip failed") end
        if light.Strength ~= 0.5 then error(label .. ".Strength round-trip failed") end
    end

    local global = Instance.new("GlobalLight", game)
    if global.ClassName ~= "GlobalLight" then error("GlobalLight ClassName mismatch") end
    checkBase(global, "GlobalLight")

    local point = Instance.new("PointLight", game)
    if point.ClassName ~= "PointLight" then error("PointLight ClassName mismatch") end
    checkBase(point, "PointLight")

    local spot = Instance.new("Spotlight", game)
    if spot.ClassName ~= "Spotlight" then error("Spotlight ClassName mismatch") end
    checkBase(spot, "Spotlight")
    if type(spot.Angle) ~= "number" then error("Spotlight.Angle missing") end
    spot.Angle = 30
    if spot.Angle ~= 30 then error("Spotlight.Angle round-trip failed") end

    local area = Instance.new("AreaLight", game)
    if area.ClassName ~= "AreaLight" then error("AreaLight ClassName mismatch") end
    checkBase(area, "AreaLight")
    if typeof(area.Size) ~= "Vector2" then error("AreaLight.Size must default to a Vector2") end
    area.Size = Vector2.new(2, 3)
    if area.Size ~= Vector2.new(2, 3) then error("AreaLight.Size round-trip failed") end

    -- Lights must NOT be cloneable (only BasePart, Folder, RenderGroup are).
    local cloneFailed = false
    local ok = pcall(function() global:Clone() end)
    if ok then error("GlobalLight should not be cloneable directly") end

    -- ---- Visual demo ---------------------------------------------------
    -- BasePart (and anything Renderable) defaults to Render=false, so we
    -- have to opt the subject + pivot in or no GameObjects get created.
    local subject = Instance.new("BasePart", game)
    subject.Name = "LightSubject"
    subject.Render = true
    subject.Size = Vector3.new(3, 3, 3)
    subject.CFrame = CFrame.new(Vector3.new(0, 0, 0))

    -- A pivot cube whose CFrame drives the directional light's rotation.
    -- We need Render=true so the pivot has a Unity transform for the light
    -- to be reparented under. A tiny size keeps it visually negligible.
    local pivot = Instance.new("BasePart", game)
    pivot.Name = "GlobalLightPivot"
    pivot.Render = true
    pivot.Size = Vector3.new(0.01, 0.01, 0.01)
    pivot.CFrame = CFrame.Angles(45, -30, 0)

    -- Use a strongly tinted bright light so the contribution is unmistakable
    -- even against the scene's existing ambient illumination.
    global.Color = Color3.new(0.3, 0.6, 1)   -- cool blue
    global.Intensity = 4
    global.Brightness = 1
    global.Active = true
    global.Parent = pivot

    point.Active = false
    spot.Active = false
    area.Active = false

    -- Aim the camera at the subject.
    local cam = game.CurrentCamera
    if cam then
        cam.CFrame = CFrame.new(Vector3.new(6, 4, -8), Vector3.new(20, -35, 0))
    end

    -- Hold the lit scene briefly so the lighting effect is visible.
    return 6
end

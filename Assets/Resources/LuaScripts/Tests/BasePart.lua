return function()
    local part = Instance.new("BasePart", game)
    part.Name = "TestPart"
    part.Render = true

    if part.ClassName ~= "BasePart" then error("ClassName mismatch") end
    if typeof(part.Size)   ~= "Vector3" then error("Size default missing") end
    if typeof(part.CFrame) ~= "CFrame"  then error("CFrame default missing") end
    if part.Moveable ~= true then error("BasePart should be Moveable") end
    if part.Shape ~= "Cube" then error("Shape should default to Cube") end
    if typeof(part.Color) ~= "Color3" then error("Color should default to a Color3") end
    if part.Transparency ~= 0 then error("Transparency should default to 0") end

    -- Property round-trips.
    part.Size = Vector3.new(2, 4, 6)
    if part.Size ~= Vector3.new(2, 4, 6) then error("Size round-trip failed") end

    part.CFrame = CFrame.new(Vector3.new(1, 2, 3))
    if part.CFrame ~= CFrame.new(Vector3.new(1, 2, 3)) then error("CFrame round-trip failed") end

    part.Color = Color3.new(0.25, 0.5, 0.75)
    if part.Color ~= Color3.new(0.25, 0.5, 0.75) then error("Color round-trip failed") end

    part.Transparency = 0.5
    if part.Transparency ~= 0.5 then error("Transparency round-trip failed") end

    -- Transparency clamps to [0,1].
    part.Transparency = 5
    if part.Transparency ~= 1 then error("Transparency should clamp to 1") end
    part.Transparency = -1
    if part.Transparency ~= 0 then error("Transparency should clamp to 0") end

    -- Shape variants build the matching primitive.
    for _, shape in ipairs({ "Cube", "Sphere", "Cylinder", "Capsule", "Plane", "Quad" }) do
        part.Shape = shape
        if part.Shape ~= shape then error("Shape round-trip failed for " .. shape) end
    end

    local okShape = pcall(function() part.Shape = "Pyramid" end)
    if okShape then error("Unknown Shape must error") end

    -- Bad types are rejected.
    local ok1 = pcall(function() part.Size = 5 end)
    if ok1 then error("Size = number must error") end
    local ok2 = pcall(function() part.CFrame = Vector3.new(0, 0, 0) end)
    if ok2 then error("CFrame = Vector3 must error") end
    local ok3 = pcall(function() part.Color = "red" end)
    if ok3 then error("Color = string must error") end

    -- Shadable defaults.
    if part.CastShadow ~= true   then error("CastShadow should default true") end
    if part.ReceiveShadow ~= true then error("ReceiveShadow should default true") end

    part.CastShadow = false
    if part.CastShadow ~= false then error("CastShadow round-trip failed") end

    -- Movement propagates to descendants — child part follows parent.
    local child = Instance.new("BasePart", part)
    child.Render = true
    child.CFrame = CFrame.new(Vector3.new(1, 0, 0))   -- offset from parent

    -- Move the parent +10 along X. The child should move by the same delta.
    part.CFrame = CFrame.new(Vector3.new(11, 2, 3))
    if child.CFrame.Position.X < 10 then
        error("Child should follow parent's CFrame delta, got X=" .. tostring(child.CFrame.Position.X))
    end

    -- Clone should produce an independent BasePart with the same properties.
    local clone = part:Clone()
    if clone.ClassName ~= "BasePart" then error("Clone ClassName mismatch") end
    if clone.Size ~= part.Size then error("Clone Size mismatch") end
    if clone.CFrame ~= part.CFrame then error("Clone CFrame mismatch") end
    if clone.CastShadow ~= part.CastShadow then error("Clone CastShadow mismatch") end
    if clone.Shape ~= part.Shape then error("Clone Shape mismatch") end
    if clone.Color ~= part.Color then error("Clone Color mismatch") end
    if clone.Transparency ~= part.Transparency then error("Clone Transparency mismatch") end

    -- Hold the rendered part on screen for a few seconds before main.lua
    -- destroys everything between tests.
    return 3
end

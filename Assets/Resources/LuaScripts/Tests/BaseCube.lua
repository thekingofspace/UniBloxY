return function()
    local cube = Instance.new("BaseCube", game)
    cube.Name = "TestCube"
    cube.Render = true

    if cube.ClassName ~= "BaseCube" then error("ClassName mismatch") end
    if typeof(cube.Size)   ~= "Vector3" then error("Size default missing") end
    if typeof(cube.CFrame) ~= "CFrame"  then error("CFrame default missing") end
    if cube.Moveable ~= true then error("BaseCube should be Moveable") end

    -- Property round-trips.
    cube.Size = Vector3.new(2, 4, 6)
    if cube.Size ~= Vector3.new(2, 4, 6) then error("Size round-trip failed") end

    cube.CFrame = CFrame.new(Vector3.new(1, 2, 3))
    if cube.CFrame ~= CFrame.new(Vector3.new(1, 2, 3)) then error("CFrame round-trip failed") end

    -- Bad types are rejected.
    local ok1 = pcall(function() cube.Size = 5 end)
    if ok1 then error("Size = number must error") end
    local ok2 = pcall(function() cube.CFrame = Vector3.new(0, 0, 0) end)
    if ok2 then error("CFrame = Vector3 must error") end

    -- Shadable defaults.
    if cube.CastShadow ~= true   then error("CastShadow should default true") end
    if cube.ReceiveShadow ~= true then error("ReceiveShadow should default true") end

    cube.CastShadow = false
    if cube.CastShadow ~= false then error("CastShadow round-trip failed") end

    -- Movement propagates to descendants — child cube follows parent.
    local child = Instance.new("BaseCube", cube)
    child.Render = true
    child.CFrame = CFrame.new(Vector3.new(1, 0, 0))   -- offset from parent

    -- Move the parent +10 along X. The child should move by the same delta.
    cube.CFrame = CFrame.new(Vector3.new(11, 2, 3))
    if child.CFrame.Position.X < 10 then
        error("Child should follow parent's CFrame delta, got X=" .. tostring(child.CFrame.Position.X))
    end

    -- Clone should produce an independent BaseCube with the same properties.
    local clone = cube:Clone()
    if clone.ClassName ~= "BaseCube" then error("Clone ClassName mismatch") end
    if clone.Size ~= cube.Size then error("Clone Size mismatch") end
    if clone.CFrame ~= cube.CFrame then error("Clone CFrame mismatch") end
    if clone.CastShadow ~= cube.CastShadow then error("Clone CastShadow mismatch") end

    -- Hold the rendered cube on screen for a few seconds before main.lua
    -- destroys everything between tests.
    return 3
end

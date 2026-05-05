return function()
    local rg = Instance.new("RenderGroup", game)
    rg.Name = "TestGroup"

    if rg.ClassName ~= "RenderGroup" then error("ClassName mismatch") end

    -- Render flag (inherited from Renderable).
    if type(rg.Render) ~= "boolean" then error("Render flag missing") end
    rg.Render = true
    if rg.Render ~= true then error("Render round-trip failed") end
    rg.Render = false
    if rg.Render ~= false then error("Render reset failed") end

    -- OverrideParent flag.
    if type(rg.OverrideParent) ~= "boolean" then error("OverrideParent missing") end
    rg.OverrideParent = true
    if rg.OverrideParent ~= true then error("OverrideParent round-trip failed") end

    -- Bad types rejected.
    local ok = pcall(function() rg.OverrideParent = "yes" end)
    if ok then error("OverrideParent = string must error") end

    -- Children of a RenderGroup are reachable like any Instance.
    local cube = Instance.new("BaseCube", rg)
    if rg:FindFirstChild(cube.Name) ~= cube then error("child lookup failed") end

    -- RenderGroup is clonable; the clone preserves OverrideParent.
    local copy = rg:Clone()
    if copy.ClassName ~= "RenderGroup" then error("Clone ClassName mismatch") end
    if copy.OverrideParent ~= true then error("Clone should preserve OverrideParent") end

    -- Hold the rendered group briefly so its child cubes stay on screen.
    rg.Render = true
    return 3
end

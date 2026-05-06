return function()
    -- =========================================================================
    -- Visible "enemy" cube the bar floats above. Render=true so the part has
    -- a Unity transform the BillboardFollower can project from.
    -- =========================================================================
    local cube = Instance.new("BasePart", game)
    cube.Name = "Enemy"
    cube.Render = true
    cube.Size = Vector3.new(2, 2, 2)
    cube.CFrame = CFrame.new(Vector3.new(0, 1, 0))
    cube.Color = Color3.fromRGB(120, 30, 30)

    -- Camera aims at the bar's projected world point (cube center + bar offset)
    -- so the healthbar lands near the middle of the screen.
    local barOffset = Vector3.new(0, 1.75, 0)
    local barWorld = Vector3.new(0, 1, 0) + barOffset
    local cam = game.CurrentCamera
    if cam then
        cam.CFrame = CFrame.LookAt(Vector3.new(0, barWorld.Y, -8), barWorld)
    end

    -- =========================================================================
    -- BillboardGui: a 200x24 canvas riding above the cube.
    -- =========================================================================
    local bb = Instance.new("BillboardGui", cube)
    bb.Name = "HealthBar"
    bb.Size = UDim2.new(0, 200, 0, 24)
    bb.Offset = barOffset
    bb.AlwaysOnTop = true

    -- Dark "missing health" track filling the whole canvas.
    local track = Instance.new("Frame", bb)
    track.Name = "Track"
    track.Size = UDim2.fromScale(1, 1)
    track.Position = UDim2.fromOffset(0, 0)
    track.BackgroundColor = Color3.fromRGB(30, 10, 10)

    -- Green "current health" fill, shrinks left-to-right as health drains.
    -- Default anchorPoint (0,0) pins the pivot to the top-left, so reducing
    -- the X scale makes the right edge retract toward the left.
    local fill = Instance.new("Frame", track)
    fill.Name = "Fill"
    fill.Size = UDim2.fromScale(1, 1)
    fill.Position = UDim2.fromOffset(0, 0)
    fill.BackgroundColor = Color3.fromRGB(50, 200, 70)

    -- =========================================================================
    -- Drain the bar from 100% to 0% over a few seconds, tinting from green to
    -- red as it drops so the change is obvious.
    -- =========================================================================
    local thread = coroutine.running()
    local elapsed = 0
    local duration = 4
    local hold = 0.75

    local conn
    conn = RunService.Heartbeat:Connect(function(dt)
        elapsed = elapsed + dt
        local pct = math.max(0, math.min(1, 1 - elapsed / duration))

        fill.Size = UDim2.new(pct, 0, 1, 0)

        local r = math.floor(50 + (1 - pct) * 180)
        local g = math.floor(50 + pct * 170)
        fill.BackgroundColor = Color3.fromRGB(r, g, 60)

        if elapsed >= duration + hold then
            conn:Disconnect()
            coroutine.resume(thread)
        end
    end)

    coroutine.yield()

    return 0
end

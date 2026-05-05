return function()
    -- Two stationary cubes for the traveler to visit. They give the moving
    -- light something to fall on so we can see the highlight slide across.
    local left = Instance.new("BaseCube", game)
    left.Name = "LeftCube"
    left.Render = true
    left.Size = Vector3.new(3, 3, 3)
    left.CFrame = CFrame.new(Vector3.new(-5, 0, 0))

    local right = Instance.new("BaseCube", game)
    right.Name = "RightCube"
    right.Render = true
    right.Size = Vector3.new(3, 3, 3)
    right.CFrame = CFrame.new(Vector3.new(5, 0, 0))

    -- Tiny traveler cube that carries a PointLight as a child. With
    -- LightElement's ParentsUnityObject = true, Unity's transform hierarchy
    -- automatically drags the light along when the traveler moves.
    local traveler = Instance.new("BaseCube", game)
    traveler.Name = "Traveler"
    traveler.Render = true
    traveler.Size = Vector3.new(0.5, 0.5, 0.5)
    traveler.CFrame = CFrame.new(Vector3.new(-5, 3, 0))

    local lamp = Instance.new("PointLight", traveler)
    lamp.Name = "TravelerLamp"
    lamp.Color = Color3.new(1, 0.6, 0.1)   -- warm orange
    lamp.Intensity = 5
    lamp.Range = 10
    lamp.Active = true

    -- Aim the camera so both cubes plus the traveler's path are in view.
    local cam = game.CurrentCamera
    if cam then
        cam.CFrame = CFrame.new(Vector3.new(0, 6, -12), Vector3.new(20, 0, 0))
    end

    local duration = 5
    local startPos = Vector3.new(-5, 3, 0)
    local endPos   = Vector3.new( 5, 3, 0)

    -- Animate the traveler's CFrame across the two endpoints in a heartbeat-
    -- driven thread, ping-ponging so the light visits both cubes during the
    -- demo window. Disconnects itself once the duration elapses.
    local elapsed = 0
    local conn
    conn = RunService.Heartbeat:Connect(function(dt)
        elapsed = elapsed + dt
        local t = math.min(elapsed / duration, 1)
        local p = (t < 0.5) and (t * 2) or (2 - t * 2)   -- 0→1→0
        traveler.CFrame = CFrame.new(startPos:Lerp(endPos, p))
        if elapsed >= duration then
            conn:Disconnect()
        end
    end)

    -- Hold the scene for the animation duration plus a one-second buffer so
    -- the final frame is observable before the next test wipes the children.
    return duration + 1
end

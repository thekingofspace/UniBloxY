return function()
    -- =========================================================================
    -- BillboardGui needs a 3D source to follow; spawn a rendered BasePart
    -- and aim the camera at it so the billboard projects somewhere visible.
    -- =========================================================================
    -- Host part is rendered (so it has a Unity transform the follower can read)
    -- but fully transparent, so it doesn't block the billboard visually.
    ---@type BasePart
    local part = Instance.new("BasePart", game)
    part.Name = "BillboardHost"
    part.Render = true
    part.Transparency = 1
    part.Size = Vector3.new(5, 5, 5)
    part.CFrame = CFrame.new(Vector3.new(0, 2.5, 0))
    part.Color = Color3.fromRGB(255, 255, 255)

    -- Aim the camera at where the billboard will project (cube center + the
    -- final Offset of (0, 2, 0)) so it lands near the middle of the screen.
    local cam = game.CurrentCamera
    if cam then
        cam.CFrame = CFrame.LookAt(Vector3.new(0, 4.5, -10), Vector3.new(0, 4.5, 0))
    end

    -- =========================================================================
    -- Construction + property defaults.
    -- =========================================================================
    local bb = Instance.new("BillboardGui", part)
    bb.Name = "Bubble"

    if bb.ClassName ~= "BillboardGui" then error("ClassName mismatch") end
    if bb.Enabled ~= true then error("Enabled should default to true") end
    if typeof(bb.Size) ~= "UDim2" then error("Size should default to a UDim2") end
    if bb.AlwaysOnTop ~= false then error("AlwaysOnTop should default to false") end
    if typeof(bb.Offset) ~= "Vector3" then error("Offset should default to a Vector3") end
    if bb.Offset ~= Vector3.new(0, 0, 0) then
        error("Offset should default to (0,0,0), got " .. tostring(bb.Offset))
    end

    -- GUIBase fields are still reachable through inheritance.
    if type(bb.Visible) ~= "boolean" then error("BillboardGui should expose Visible from GUIBase") end

    -- =========================================================================
    -- Property round-trips + type guards.
    -- =========================================================================
    bb.Size = UDim2.new(0, 200, 0, 60)
    if bb.Size ~= UDim2.new(0, 200, 0, 60) then error("Size round-trip failed") end

    bb.AlwaysOnTop = true
    if bb.AlwaysOnTop ~= true then error("AlwaysOnTop round-trip failed") end

    bb.Offset = Vector3.new(0, 2, 0)
    if bb.Offset ~= Vector3.new(0, 2, 0) then error("Offset round-trip failed") end
    bb.Offset = Vector3.new(0, 0, 0)

    local okSize = pcall(function() bb.Size = "big" end)
    if okSize then error("Size = string must error") end
    local okAOT = pcall(function() bb.AlwaysOnTop = "yes" end)
    if okAOT then error("AlwaysOnTop = string must error") end
    local okOff = pcall(function() bb.Offset = 5 end)
    if okOff then error("Offset = number must error") end
    local okEn = pcall(function() bb.Enabled = 1 end)
    if okEn then error("Enabled = number must error") end

    -- Enabled mirrors into Visible so listener queries see the toggle.
    bb.Enabled = false
    if bb.Visible ~= false then error("Enabled=false should mirror into Visible") end
    bb.Enabled = true
    if bb.Visible ~= true then error("Enabled=true should mirror into Visible") end

    -- =========================================================================
    -- Children parent under the billboard's screen canvas. A small label
    -- gives us something on screen during the hold.
    -- =========================================================================
    local label = Instance.new("TextLabel", bb)
    label.Name = "BubbleText"
    label.Size = UDim2.fromScale(1, 1)
    label.Position = UDim2.fromOffset(0, 0)
    label.Text = "Hello!"
    label.TextSize = 24
    label.TextColor = Color3.new(1, 1, 1)
    label.TextScaled = true

    -- =========================================================================
    -- Listener integration: BillboardGui's UnityObject is a RectTransform
    -- canvas, so the listener accepts it (and its UI children) as 2D
    -- trackers. The contract we exercise is AddTracker / GetTrackers.
    -- =========================================================================
    local mouseL = ListenerService:ListenToMouse()
    mouseL:AddTracker(bb)
    mouseL:AddTracker(label)
    if #mouseL:GetTrackers() ~= 2 then
        error("Mouse listener should accept a BillboardGui and its child as trackers")
    end

    -- Disabling the BillboardGui hides its descendants via the Visible cascade.
    bb.Enabled = false
    if label.Visible ~= true then
        error("Disabling BillboardGui must NOT flip the descendant's own Visible flag")
    end
    bb.Enabled = true
    mouseL:Destroy()

    -- =========================================================================
    -- Reparent test: an unparented (or non-3D-parented) BillboardGui isn't
    -- visible. The test exercises the property contract — the actual canvas
    -- hide is driven by the per-frame follower, so we just verify the API
    -- handles parent transitions cleanly without erroring.
    -- =========================================================================
    bb.Parent = nil
    if bb.Parent ~= nil then error("BillboardGui should accept Parent = nil") end
    bb.Parent = part
    if bb.Parent ~= part then error("BillboardGui should accept reparenting back to a 3D part") end

    -- =========================================================================
    -- Cloning: every property carries across, plus descendants.
    -- =========================================================================
    bb.Offset = Vector3.new(0, 2, 0)
    bb.AlwaysOnTop = true
    bb.Size = UDim2.new(0, 200, 0, 60)
    local copy = bb:Clone()
    if copy == bb then error("Clone returned the same instance") end
    if copy.ClassName ~= "BillboardGui" then error("Clone ClassName mismatch") end
    if copy.Enabled ~= true then error("Clone should preserve Enabled") end
    if copy.Size ~= bb.Size then error("Clone should preserve Size") end
    if copy.AlwaysOnTop ~= true then error("Clone should preserve AlwaysOnTop") end
    if copy.Offset ~= Vector3.new(0, 2, 0) then error("Clone should preserve Offset") end
    if #copy:GetChildren() ~= 1 then
        error("Clone should preserve children, got " .. tostring(#copy:GetChildren()))
    end

    -- Park the clone next to the original so both are visible during the hold.
    copy.Parent = part
    copy.Offset = Vector3.new(2, 2, 0)

    -- =========================================================================
    -- Orbit the camera around the cube during the visual hold so the billboard
    -- follower has to keep re-aiming at the moving viewpoint.
    -- =========================================================================
    local cam = game.CurrentCamera
    if cam then
        local thread = coroutine.running()
        local elapsed = 0
        local duration = 6
        local radius = 10
        -- Both the orbit eye height and the LookAt target sit on the billboard's
        -- projected world point (cube center + bb.Offset = (0, 4.5, 0)) so it
        -- stays centered in frame across the full orbit. A small height bob
        -- keeps the motion visible without swinging the billboard off-screen.
        local target = Vector3.new(0, 4.5, 0)

        local conn
        conn = RunService.Heartbeat:Connect(function(dt)
            elapsed = elapsed + dt
            local t = elapsed
            local angle = t * math.pi * 0.5
            local pos = Vector3.new(
                math.sin(angle) * radius,
                target.Y + math.sin(t * 1.2) * 0.5,
                -math.cos(angle) * radius
            )
            cam.CFrame = CFrame.LookAt(pos, target)

            if elapsed >= duration then
                conn:Disconnect()
                coroutine.resume(thread)
            end
        end)

        coroutine.yield()
    end

    return 0
end

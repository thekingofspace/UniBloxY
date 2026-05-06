return function()
    -- =========================================================================
    -- A SurfaceGui needs a 3D parent so the world-space canvas has something
    -- to ride on. Spawn a rendered BasePart and aim the camera at it so the
    -- visual hold actually shows the painted surface.
    -- =========================================================================
    local part = Instance.new("BasePart", game)
    part.Name = "SurfaceHost"
    part.Render = true
    part.Size = Vector3.new(3, 3, 3)
    part.CFrame = CFrame.new(Vector3.new(0, 0, 0))
    part.Color = Color3.fromRGB(60, 60, 80)

    local cam = game.CurrentCamera
    if cam then
        cam.CFrame = CFrame.new(Vector3.new(0, 1, -6), Vector3.new(0, 0, 0))
    end

    -- =========================================================================
    -- Construction + property defaults.
    -- =========================================================================
    local gui = Instance.new("SurfaceGui", part)
    gui.Name = "Sign"

    if gui.ClassName ~= "SurfaceGui" then error("ClassName mismatch") end
    if gui.Wrap ~= true then error("Wrap should default to true") end
    if gui.Angle ~= 0 then error("Angle should default to 0") end
    if gui.Enabled ~= true then error("Enabled should default to true") end

    -- GUIBase fields are still reachable.
    if type(gui.Visible) ~= "boolean" then error("SurfaceGui should expose Visible from GUIBase") end

    -- =========================================================================
    -- Property round-trips + type guards.
    -- =========================================================================
    gui.Wrap = false
    if gui.Wrap ~= false then error("Wrap round-trip failed") end
    gui.Wrap = true

    gui.Angle = 45
    if gui.Angle ~= 45 then error("Angle round-trip failed") end

    -- Angle clamps to [-180, 180].
    gui.Angle = 500
    if gui.Angle ~= 180 then error("Angle should clamp to 180, got " .. tostring(gui.Angle)) end
    gui.Angle = -500
    if gui.Angle ~= -180 then error("Angle should clamp to -180, got " .. tostring(gui.Angle)) end
    gui.Angle = 0

    local okWrap = pcall(function() gui.Wrap = "yes" end)
    if okWrap then error("Wrap = string must error") end
    local okAngle = pcall(function() gui.Angle = "30" end)
    if okAngle then error("Angle = string must error") end
    local okEn = pcall(function() gui.Enabled = 1 end)
    if okEn then error("Enabled = number must error") end

    -- Enabled mirrors into Visible so listener queries (which walk the
    -- GUIBase Visible cascade) honor the toggle.
    gui.Enabled = false
    if gui.Visible ~= false then error("Enabled=false should mirror into Visible") end
    gui.Enabled = true
    if gui.Visible ~= true then error("Enabled=true should mirror into Visible") end

    -- =========================================================================
    -- Children of a SurfaceGui parent under its world-space canvas. We add a
    -- TextLabel + Frame so there's something painted on the surface during
    -- the visual hold.
    -- =========================================================================
    local label = Instance.new("TextLabel", gui)
    label.Name = "SurfaceText"
    label.Size = UDim2.new(1, 0, 0, 30)
    label.Position = UDim2.fromOffset(0, 0)
    label.Text = "SurfaceGui"
    label.TextSize = 28
    label.TextColor = Color3.new(1, 1, 1)
    label.TextScaled = true

    local panel = Instance.new("Frame", gui)
    panel.Name = "SurfacePanel"
    panel.Size = UDim2.fromScale(1, 1)
    panel.Position = UDim2.fromOffset(0, 0)
    panel.BackgroundColor = Color3.fromRGB(20, 80, 160)
    panel.BackgroundTransparency = 0.5

    -- =========================================================================
    -- Listener integration: a SurfaceGui's UnityObject has a RectTransform
    -- (its world-space Canvas), so the listener treats it as a 2D entity.
    -- AddTracker is the contract surface we care about — the listener should
    -- accept it without throwing.
    -- =========================================================================
    local mouseL = ListenerService:ListenToMouse()
    mouseL:AddTracker(gui)
    mouseL:AddTracker(label)
    if #mouseL:GetTrackers() ~= 2 then
        error("Mouse listener should accept a SurfaceGui and its child as trackers")
    end

    -- Disabling the SurfaceGui hides its descendants via the Visible cascade,
    -- so they won't fire spurious mouse hits while disabled.
    gui.Enabled = false
    if label.Visible ~= true then
        error("Disabling SurfaceGui must NOT flip the descendant's own Visible flag")
    end
    gui.Enabled = true
    mouseL:Destroy()

    -- =========================================================================
    -- Cloning: state carries across (Wrap/Angle/Enabled + descendants).
    -- =========================================================================
    gui.Angle = 30
    gui.Wrap = false
    local copy = gui:Clone()
    if copy == gui then error("Clone returned the same instance") end
    if copy.ClassName ~= "SurfaceGui" then error("Clone ClassName mismatch") end
    if copy.Wrap ~= false then error("Clone should preserve Wrap") end
    if copy.Angle ~= 30 then error("Clone should preserve Angle") end
    if copy.Enabled ~= true then error("Clone should preserve Enabled") end
    if #copy:GetChildren() ~= 2 then
        error("Clone should preserve children, got " .. tostring(#copy:GetChildren()))
    end
    copy:Destroy()

    -- Restore default-ish state for the visual hold.
    gui.Wrap = true
    gui.Angle = 0
    gui.Enabled = true

    return 6
end

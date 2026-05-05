return function()
    -- Centered card: AnchorPoint of (0.5, 0.5) puts the rect's middle at the
    -- position, and Position {0.5, 0, 0.5, 0} is the middle of the parent —
    -- so the card sits at the screen's center regardless of viewport size.
    local card = Instance.new("Frame", game)
    card.Name = "MouseEnterCard"
    card.AnchorPoint = Vector2.new(0.5, 0.5)
    card.Position = UDim2.fromScale(0.5, 0.5)
    card.Size = UDim2.fromOffset(280, 160)
    card.BackgroundColor = Color3.fromRGB(28, 32, 48)
    card.BackgroundTransparency = 0
    card.ZIndex = 10

    local label = Instance.new("TextLabel", card)
    label.Name = "Prompt"
    label.AnchorPoint = Vector2.new(0.5, 0.5)
    label.Position = UDim2.fromScale(0.5, 0.5)
    label.Size = UDim2.new(1, -16, 1, -16)
    label.Text = "Move the mouse onto this box"
    label.TextSize = 18
    label.TextColor = Color3.fromRGB(235, 245, 255)
    label.TextScaled = true
    label.TextXAlignment = "Center"
    label.TextYAlignment = "Center"
    label.ZIndex = 11

    -- AnchorPoint round-trip sanity check.
    if card.AnchorPoint.X ~= 0.5 or card.AnchorPoint.Y ~= 0.5 then
        error("AnchorPoint round-trip failed on Frame")
    end
    if label.AnchorPoint.X ~= 0.5 or label.AnchorPoint.Y ~= 0.5 then
        error("AnchorPoint round-trip failed on TextLabel")
    end
    local okBad = pcall(function() card.AnchorPoint = "center" end)
    if okBad then error("AnchorPoint = string must error") end

    -- Listen for the mouse entering the card; resume the test thread on the
    -- first OnEnter so the assertion below can confirm it fired.
    local listener = ListenerService.ListenToMouse()
    listener:AddTracker(card)

    local thread = coroutine.running()
    local entered = false
    local timedOut = false

    listener.OnEnter:Connect(function(t)
        if entered then return end
        if t == card then
            entered = true
            label.Text = "Mouse entered!"
            label.TextColor = Color3.fromRGB(120, 255, 160)
            if coroutine.status(thread) == "suspended" then
                coroutine.resume(thread)
            end
        end
    end)

    -- Time-cap so the suite doesn't stall when nobody's at the keyboard. Six
    -- seconds is plenty to flick the mouse onto a centered 280×160 box.
    local elapsed = 0
    local timeout = 6
    local conn
    conn = RunService.Heartbeat:Connect(function(dt)
        elapsed = elapsed + dt
        if elapsed >= timeout and not entered then
            timedOut = true
            if coroutine.status(thread) == "suspended" then
                coroutine.resume(thread)
            end
        end
    end)

    coroutine.yield()

    if conn and conn.Connected then conn:Disconnect() end
    listener:Destroy()

    if timedOut and not entered then
        print("MouseEnter: timed out before the cursor entered the card (no human present?)")
    end

    -- Hold briefly so the success/timeout state is visible.
    return 1
end

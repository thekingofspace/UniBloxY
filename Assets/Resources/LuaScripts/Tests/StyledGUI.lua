return function()
    -- Stylized UI: composited Frames + TextLabels showcasing the GUI hierarchy
    -- and ShadableUI's shader hook. Layout is anchored to a centered card so
    -- it reads regardless of the viewport size.

    -- ==============================================
    -- Backdrop: full-screen dim panel.
    -- ==============================================
    local backdrop = Instance.new("Frame", game)
    backdrop.Name = "Backdrop"
    backdrop.Size = UDim2.fromScale(1, 1)
    backdrop.Position = UDim2.fromOffset(0, 0)
    backdrop.BackgroundColor = Color3.fromRGB(8, 10, 18)
    backdrop.BackgroundTransparency = 0.15
    backdrop.ZIndex = 0

    -- ==============================================
    -- Card: the centered hero panel. We position it with a fixed offset and
    -- count on the screen being at least 1024×768 in tests; if not, the card
    -- still draws — just clipped by the canvas.
    -- ==============================================
    local card = Instance.new("Frame", backdrop)
    card.Name = "Card"
    card.Size = UDim2.fromOffset(560, 360)
    card.Position = UDim2.fromOffset(232, 154)
    card.BackgroundColor = Color3.fromRGB(20, 24, 38)
    card.BackgroundTransparency = 0.05
    card.ClipDescendants = true
    card.ZIndex = 1

    -- A neon accent strip down the left edge.
    local accent = Instance.new("Frame", card)
    accent.Name = "Accent"
    accent.Size = UDim2.fromOffset(6, 360)
    accent.Position = UDim2.fromOffset(0, 0)
    accent.BackgroundColor = Color3.fromRGB(80, 200, 255)
    accent.BackgroundTransparency = 0
    accent.ZIndex = 3

    -- A subtle header bar.
    local header = Instance.new("Frame", card)
    header.Name = "Header"
    header.Size = UDim2.new(1, 0, 0, 56)
    header.Position = UDim2.fromOffset(0, 0)
    header.BackgroundColor = Color3.fromRGB(14, 18, 30)
    header.BackgroundTransparency = 0
    header.ZIndex = 2

    -- ==============================================
    -- Headlines.
    -- ==============================================
    local title = Instance.new("TextLabel", card)
    title.Name = "Title"
    title.Size = UDim2.new(1, -32, 0, 56)
    title.Position = UDim2.fromOffset(24, 0)
    title.Text = "WELCOME"
    title.TextSize = 28
    title.TextColor = Color3.fromRGB(235, 245, 255)
    title.TextXAlignment = "Left"
    title.TextYAlignment = "Center"
    title.ZIndex = 4

    local subtitle = Instance.new("TextLabel", card)
    subtitle.Name = "Subtitle"
    subtitle.Size = UDim2.new(1, -48, 0, 32)
    subtitle.Position = UDim2.fromOffset(24, 76)
    subtitle.Text = "Stylized UI Demo"
    subtitle.TextSize = 18
    subtitle.TextColor = Color3.fromRGB(140, 170, 210)
    subtitle.TextXAlignment = "Left"
    subtitle.TextYAlignment = "Center"
    subtitle.ZIndex = 4

    local body = Instance.new("TextLabel", card)
    body.Name = "Body"
    body.Size = UDim2.new(1, -48, 0, 96)
    body.Position = UDim2.fromOffset(24, 120)
    body.Text = "Frames, TextLabels, and ImageLabels composed\ntogether with a custom UI shader for the action panel below."
    body.TextSize = 14
    body.TextColor = Color3.fromRGB(190, 200, 220)
    body.TextXAlignment = "Left"
    body.TextYAlignment = "Top"
    body.TextScaled = false
    body.ZIndex = 4

    -- ==============================================
    -- Action button: a Frame styled with the UIGradient shader so the
    -- ShadableUI surface gets exercised end-to-end.
    -- ==============================================
    local button = Instance.new("Frame", card)
    button.Name = "ActionButton"
    button.Size = UDim2.fromOffset(220, 56)
    button.Position = UDim2.fromOffset(24, 264)
    button.BackgroundColor = Color3.fromRGB(255, 255, 255)
    button.BackgroundTransparency = 0
    button.ZIndex = 5

    local buttonLabel = Instance.new("TextLabel", button)
    buttonLabel.Name = "Label"
    buttonLabel.Size = UDim2.fromScale(1, 1)
    buttonLabel.Position = UDim2.fromOffset(0, 0)
    buttonLabel.Text = "PLAY"
    buttonLabel.TextSize = 22
    buttonLabel.TextColor = Color3.fromRGB(255, 255, 255)
    buttonLabel.TextXAlignment = "Center"
    buttonLabel.TextYAlignment = "Center"
    buttonLabel.ZIndex = 6

    -- A second "ghost" button next to the primary action — same shader, but
    -- tinted differently to show that SetShaderData mutations are per-instance.
    local ghostButton = Instance.new("Frame", card)
    ghostButton.Name = "GhostButton"
    ghostButton.Size = UDim2.fromOffset(160, 56)
    ghostButton.Position = UDim2.fromOffset(260, 264)
    ghostButton.BackgroundColor = Color3.fromRGB(255, 255, 255)
    ghostButton.BackgroundTransparency = 0
    ghostButton.ZIndex = 5

    local ghostLabel = Instance.new("TextLabel", ghostButton)
    ghostLabel.Name = "Label"
    ghostLabel.Size = UDim2.fromScale(1, 1)
    ghostLabel.Position = UDim2.fromOffset(0, 0)
    ghostLabel.Text = "LATER"
    ghostLabel.TextSize = 18
    ghostLabel.TextColor = Color3.fromRGB(220, 230, 245)
    ghostLabel.TextXAlignment = "Center"
    ghostLabel.TextYAlignment = "Center"
    ghostLabel.ZIndex = 6

    local okShader, shader = pcall(function() return AssetService:GetShader("UIGradient") end)
    if okShader and shader then
        if shader.ClassName ~= "Shader" then error("AssetService:GetShader returned non-Shader") end

        button:AddShader(shader)
        if #button:ListShaders() ~= 1 then error("AddShader did not register on primary button") end
        button:SetShaderData(shader, "_Color",     Color3.fromRGB(80, 200, 255))
        button:SetShaderData(shader, "_ColorB",    Color3.fromRGB(40, 90, 220))
        button:SetShaderData(shader, "_GlowColor", Color3.fromRGB(120, 220, 255))
        button:SetShaderData(shader, "_GlowPower", 5)
        button:SetShaderData(shader, "_Sheen",     0.35)

        -- Ghost button: muted purple with a softer glow, proving each instance
        -- of ShadableUI gets its own material clone — primary stays cyan.
        ghostButton:AddShader(shader)
        ghostButton:SetShaderData(shader, "_Color",     Color3.fromRGB(120, 90, 200))
        ghostButton:SetShaderData(shader, "_ColorB",    Color3.fromRGB(40, 30, 90))
        ghostButton:SetShaderData(shader, "_GlowColor", Color3.fromRGB(200, 150, 255))
        ghostButton:SetShaderData(shader, "_GlowPower", 3)
        ghostButton:SetShaderData(shader, "_Sheen",     0.15)

        -- The accent strip too — paint it as a vertical neon bar.
        accent:AddShader(shader)
        accent:SetShaderData(shader, "_Color",     Color3.fromRGB(80, 220, 255))
        accent:SetShaderData(shader, "_ColorB",    Color3.fromRGB(150, 80, 255))
        accent:SetShaderData(shader, "_GlowColor", Color3.fromRGB(180, 240, 255))
        accent:SetShaderData(shader, "_GlowPower", 8)
        accent:SetShaderData(shader, "_Sheen",     0.5)

        if #button:ListShaders()      ~= 1 then error("Primary should have 1 shader attached") end
        if #ghostButton:ListShaders() ~= 1 then error("Ghost should have 1 shader attached") end
        if #accent:ListShaders()      ~= 1 then error("Accent should have 1 shader attached") end
    end

    -- Hero image slot — uses an ImageLabel even when no Image asset is
    -- available, so the layout stays consistent. With ImageTransparency = 1
    -- the slot draws nothing but reserves the rect.
    local heroSlot = Instance.new("ImageLabel", card)
    heroSlot.Name = "HeroSlot"
    heroSlot.Size = UDim2.fromOffset(80, 80)
    heroSlot.Position = UDim2.fromOffset(456, 12)
    heroSlot.ImageTransparency = 1
    heroSlot.ZIndex = 4

    local okImg, hero = pcall(function() return AssetService:GetImage("Logo") end)
    if okImg and hero then
        heroSlot.Image = hero
        heroSlot.ImageTransparency = 0
        heroSlot.ImageColor = Color3.fromRGB(255, 255, 255)
    end

    -- Sanity-check: structure built up the way we expect.
    if #card:GetChildren() < 6 then error("Card should contain accent, header, title, subtitle, body, buttons + slot") end
    if title.Parent ~= card then error("Title parented incorrectly") end
    if buttonLabel.Parent ~= button then error("Button label parented incorrectly") end

    -- Hold a few seconds so the styled UI is on screen long enough to inspect.
    return 4
end

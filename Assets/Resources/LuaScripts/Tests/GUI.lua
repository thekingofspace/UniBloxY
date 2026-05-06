return function()
    -- =========================================================================
    -- Visual anchor: a big white cube behind the on-screen GUI so the test is
    -- obviously running when watched live.
    -- =========================================================================
    local backdrop = Instance.new("BasePart", game)
    backdrop.Name = "GUIBackdrop"
    backdrop.Render = true
    backdrop.Size = Vector3.new(5, 5, 5)
    backdrop.CFrame = CFrame.new(Vector3.new(0, 2.5, 0))
    backdrop.Color = Color3.fromRGB(255, 255, 255)

    local cam = game.CurrentCamera
    if cam then
        cam.CFrame = CFrame.LookAt(Vector3.new(0, 4, -12), Vector3.new(0, 2.5, 0))
    end

    -- =========================================================================
    -- GUIBase: Visible + ZIndex live on every GUI element.
    -- =========================================================================
    local frame = Instance.new("Frame", game)
    frame.Name = "RootFrame"

    if frame.Visible ~= true then error("Visible should default to true") end
    if frame.ZIndex ~= 0 then error("ZIndex should default to 0") end

    frame.Visible = false
    if frame.Visible ~= false then error("Visible round-trip failed") end
    frame.Visible = true

    frame.ZIndex = 5
    if frame.ZIndex ~= 5 then error("ZIndex round-trip failed") end

    local okV = pcall(function() frame.Visible = "yes" end)
    if okV then error("Visible = string must error") end
    local okZ = pcall(function() frame.ZIndex = "5" end)
    if okZ then error("ZIndex = string must error") end

    -- =========================================================================
    -- Frame properties: Size, Position, BackgroundColor, BackgroundTransparency,
    -- ClipDescendants.
    -- =========================================================================
    frame.Size = UDim2.new(0, 400, 0, 300)
    frame.Position = UDim2.fromOffset(20, 20)
    frame.BackgroundColor = Color3.fromRGB(40, 40, 60)
    frame.BackgroundTransparency = 0.25
    frame.ClipDescendants = true

    if frame.BackgroundTransparency ~= 0.25 then error("BackgroundTransparency round-trip failed") end
    if frame.ClipDescendants ~= true then error("ClipDescendants round-trip failed") end
    if frame.BackgroundColor3 ~= frame.BackgroundColor then error("BackgroundColor3 alias mismatch") end

    -- BackgroundTransparency clamps to [0,1].
    frame.BackgroundTransparency = 5
    if frame.BackgroundTransparency ~= 1 then error("BackgroundTransparency should clamp to 1") end
    frame.BackgroundTransparency = -1
    if frame.BackgroundTransparency ~= 0 then error("BackgroundTransparency should clamp to 0") end

    local okSize = pcall(function() frame.Size = "big" end)
    if okSize then error("Size = string must error") end
    local okClip = pcall(function() frame.ClipDescendants = 1 end)
    if okClip then error("ClipDescendants = number must error") end

    -- =========================================================================
    -- TextLabel: text, font (optional), text size/color/transparency, scaled,
    -- alignment. No background field — it renders text only.
    -- =========================================================================
    local label = Instance.new("TextLabel", frame)
    label.Name = "Title"
    label.Size = UDim2.new(1, 0, 0, 40)
    label.Position = UDim2.fromOffset(0, 0)
    label.Text = "Hello GUI"
    label.TextSize = 24
    label.TextColor = Color3.new(1, 1, 1)
    label.TextTransparency = 0
    label.TextScaled = true
    label.TextXAlignment = "Center"
    label.TextYAlignment = "Top"

    if label.Text ~= "Hello GUI" then error("Text round-trip failed") end
    if label.TextSize ~= 24 then error("TextSize round-trip failed") end
    if label.TextScaled ~= true then error("TextScaled round-trip failed") end
    if label.TextXAlignment ~= "Center" then error("TextXAlignment round-trip failed") end
    if label.BackgroundColor ~= nil then
        -- TextLabel must NOT expose a BackgroundColor — it's text-only.
        error("TextLabel should not have a BackgroundColor property")
    end

    local okText = pcall(function() label.Text = 123 end)
    if okText then error("Text = number must error") end
    local okFont = pcall(function() label.Font = "MyFont" end)
    if okFont then error("Font = string must error (must be a Font from AssetService:GetFont)") end

    -- A built-in font load is platform-dependent; if it works, it should round-trip.
    local okGetFont, font = pcall(function() return AssetService:GetFont("LegacyRuntime.ttf") end)
    if okGetFont and font then
        if font.ClassName ~= "Font" then error("AssetService:GetFont should return a Font") end
        label.Font = font
        if label.Font ~= font then error("Font round-trip failed") end
        label.Font = nil
        if label.Font ~= nil then error("Font = nil should clear the font") end
    end

    -- =========================================================================
    -- ImageLabel: Image, ImageColor, ImageTransparency. No background unless
    -- an Image is assigned.
    -- =========================================================================
    local img = Instance.new("ImageLabel", frame)
    img.Name = "Pic"
    img.Size = UDim2.new(0, 100, 0, 100)
    img.Position = UDim2.fromOffset(20, 60)
    img.ImageColor = Color3.fromRGB(255, 200, 200)
    img.ImageTransparency = 0.5

    if img.ImageTransparency ~= 0.5 then error("ImageTransparency round-trip failed") end
    if img.Image ~= nil then error("Image should default to nil") end
    if img.BackgroundColor ~= nil then error("ImageLabel should not have a BackgroundColor property") end

    local okImg = pcall(function() img.Image = "logo.png" end)
    if okImg then error("Image = string must error (must be an Image from AssetService:GetImage)") end

    img.Image = nil
    if img.Image ~= nil then error("Image = nil should round-trip") end

    -- =========================================================================
    -- ShadableUI surface: Frame/TextLabel/ImageLabel each expose the shader
    -- + material API (parity with Shadable on 3D objects).
    -- =========================================================================
    for _, e in ipairs({ frame, label, img }) do
        if type(e.AddShader)           ~= "function" then error("AddShader missing on " .. e.ClassName) end
        if type(e.RemoveShader)        ~= "function" then error("RemoveShader missing on " .. e.ClassName) end
        if type(e.ListShaders)         ~= "function" then error("ListShaders missing on " .. e.ClassName) end
        if type(e.SetShaderData)       ~= "function" then error("SetShaderData missing on " .. e.ClassName) end
        if type(e.AddMaterial)         ~= "function" then error("AddMaterial missing on " .. e.ClassName) end
        if type(e.RemoveMaterial)      ~= "function" then error("RemoveMaterial missing on " .. e.ClassName) end
        if type(e.ListMaterials)       ~= "function" then error("ListMaterials missing on " .. e.ClassName) end
        if type(e.SetMaterialProperty) ~= "function" then error("SetMaterialProperty missing on " .. e.ClassName) end

        local sh = e:ListShaders()
        local mt = e:ListMaterials()
        if type(sh) ~= "table" or #sh ~= 0 then error("ListShaders should be empty by default") end
        if type(mt) ~= "table" or #mt ~= 0 then error("ListMaterials should be empty by default") end
    end

    -- If the project ships a Default shader, exercise the shader pipeline on a
    -- Frame the same way Shadable.lua does for BasePart.
    local okShader, shader = pcall(function() return AssetService:GetShader("Default") end)
    if okShader and shader then
        frame:AddShader(shader)
        if #frame:ListShaders() ~= 1 then error("AddShader did not register on Frame") end
        frame:AddShader(shader)
        if #frame:ListShaders() ~= 1 then error("AddShader should be idempotent on Frame") end

        frame:SetShaderData(shader, "_TestFloat", 0.5)
        frame:SetShaderData(shader, "_TestColor", Color3.new(1, 0, 0))

        local okMissing = pcall(function() frame:SetShaderData(shader, "", 1) end)
        if okMissing then error("SetShaderData with empty name must error") end

        if not frame:RemoveShader(shader) then error("RemoveShader should return true") end
        if #frame:ListShaders() ~= 0 then error("RemoveShader did not detach") end
    end

    -- =========================================================================
    -- Visibility cascades down the GUI subtree (ancestors hidden ⇒ descendants
    -- effectively hidden) without mutating the descendant's own Visible flag.
    -- =========================================================================
    label.Visible = true
    frame.Visible = false
    if label.Visible ~= true then
        error("Hiding a parent must not flip the child's Visible flag")
    end
    frame.Visible = true

    -- =========================================================================
    -- Cloning preserves GUI state (Visible, ZIndex, Size, Position, etc.).
    -- =========================================================================
    frame.ZIndex = 7
    frame.Visible = false
    local copy = frame:Clone()
    if copy == frame then error("Clone returned the same instance") end
    if copy.ClassName ~= "Frame" then error("Clone ClassName mismatch") end
    if copy.ZIndex ~= 7 then error("Clone should preserve ZIndex") end
    if copy.Visible ~= false then error("Clone should preserve Visible") end
    if copy.ClipDescendants ~= true then error("Clone should preserve ClipDescendants") end

    local copyChildren = copy:GetChildren()
    if #copyChildren ~= 2 then
        error("Clone should preserve children (got " .. tostring(#copyChildren) .. ", expected 2)")
    end

    frame.Visible = true

    -- Park the clone in the scene next to the original so the on-screen GUIs
    -- show side by side during the visual hold.
    copy.Parent = game
    copy.Visible = true
    copy.Position = UDim2.fromOffset(440, 20)

    -- =========================================================================
    -- Hold briefly so the rendered GUIs are visible to anyone watching.
    -- =========================================================================
    return 6
end

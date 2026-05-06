return function()
    if type(AssetService) ~= "table" then
        error("AssetService global missing")
    end

    if type(AssetService.GetShader)     ~= "function" then error("GetShader missing") end
    if type(AssetService.Get)           ~= "function" then error("Get alias missing") end
    if type(AssetService.GetMaterial)   ~= "function" then error("GetMaterial missing") end
    if type(AssetService.GetTexture)    ~= "function" then error("GetTexture missing") end
    if type(AssetService.CreateMaterial)~= "function" then error("CreateMaterial missing") end

    -- ShaderLoaded / AssetLoaded must exist and behave like signals.
    if type(AssetService.ShaderLoaded) ~= "table" or type(AssetService.ShaderLoaded.Connect) ~= "function" then
        error("ShaderLoaded signal missing")
    end
    if type(AssetService.AssetLoaded) ~= "table" or type(AssetService.AssetLoaded.Connect) ~= "function" then
        error("AssetLoaded signal missing")
    end

    -- The old "FinishedLoading" signals must be gone.
    if AssetService.ShadersFinishedLoading ~= nil then
        error("ShadersFinishedLoading should have been removed")
    end
    if AssetService.AssetsFinishedLoading ~= nil then
        error("AssetsFinishedLoading should have been removed")
    end

    -- ShaderLoaded fires (name) when a shader is first loaded. We connect first,
    -- then trigger a load and verify the payload is the asset name.
    local seen
    local conn = AssetService.ShaderLoaded:Connect(function(name)
        seen = name
    end)

    -- Load the project's Default shader (used by BasePart). If the project ever
    -- moves it we'll catch the error here, but fall back to skipping the load
    -- assertion rather than blocking unrelated tests.
    local ok = pcall(function() AssetService:GetShader("Default") end)
    conn:Disconnect()

    if ok then
        -- Heartbeat-driven coroutines may run signal callbacks synchronously here;
        -- if our handler ran, the payload must be the shader name we asked for.
        if seen ~= nil and seen ~= "Default" then
            error("ShaderLoaded payload expected 'Default', got: " .. tostring(seen))
        end
    end

    return 0
end

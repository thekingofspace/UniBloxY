return function()
    -- ShaderService is now a thin shim that delegates to AssetService.
    if type(ShaderService) ~= "table" then error("ShaderService global missing") end

    -- The shim still exposes the loader functions.
    for _, name in ipairs({ "GetShader", "Get", "GetMaterial", "GetTexture", "CreateMaterial" }) do
        if type(ShaderService[name]) ~= "function" then
            error("ShaderService." .. name .. " missing")
        end
    end

    -- Loading via the shim should reach the same cached LuaShader the AssetService returns.
    local okA, viaAsset  = pcall(function() return AssetService:GetShader("Default") end)
    local okS, viaShim   = pcall(function() return ShaderService:GetShader("Default") end)
    if okA ~= okS then error("ShaderService and AssetService disagree on availability") end
    if okA and viaAsset ~= viaShim then
        error("ShaderService:GetShader did not delegate to the cached AssetService shader")
    end

    return 0
end

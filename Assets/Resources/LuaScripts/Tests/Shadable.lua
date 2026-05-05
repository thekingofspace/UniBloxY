return function()
    -- A BaseCube is a Shadable, so we use it as the test subject.
    local cube = Instance.new("BaseCube", game)
    cube.Render = true   -- otherwise no Unity object is created and shaders have nothing to bind to

    -- Shadable methods exist.
    if type(cube.AddShader)         ~= "function" then error("AddShader missing") end
    if type(cube.RemoveShader)      ~= "function" then error("RemoveShader missing") end
    if type(cube.ListShaders)       ~= "function" then error("ListShaders missing") end
    if type(cube.SetShaderData)     ~= "function" then error("SetShaderData missing") end
    if type(cube.AddMaterial)       ~= "function" then error("AddMaterial missing") end
    if type(cube.RemoveMaterial)    ~= "function" then error("RemoveMaterial missing") end
    if type(cube.ListMaterials)     ~= "function" then error("ListMaterials missing") end
    if type(cube.SetMaterialProperty) ~= "function" then error("SetMaterialProperty missing") end
    if type(cube.SetMaterialData)   ~= "function" then error("SetMaterialData missing") end

    -- ListShaders / ListMaterials default to empty tables.
    local sh = cube:ListShaders()
    local mt = cube:ListMaterials()
    if type(sh) ~= "table" or #sh ~= 0 then error("ListShaders should be empty by default") end
    if type(mt) ~= "table" or #mt ~= 0 then error("ListMaterials should be empty by default") end

    -- Shadow flags.
    cube.CastShadow = false
    cube.ReceiveShadow = false
    if cube.CastShadow ~= false then error("CastShadow round-trip failed") end
    if cube.ReceiveShadow ~= false then error("ReceiveShadow round-trip failed") end

    local okCast = pcall(function() cube.CastShadow = "no" end)
    if okCast then error("CastShadow = string must error") end

    -- Try loading a shader; if Default isn't present, skip the rest.
    local okLoad, shader = pcall(function() return AssetService:GetShader("Default") end)
    if okLoad and shader then
        cube:AddShader(shader)
        local list = cube:ListShaders()
        if #list ~= 1 then error("AddShader did not register the shader") end

        -- AddShader on the same shader is idempotent.
        cube:AddShader(shader)
        if #cube:ListShaders() ~= 1 then error("AddShader should be idempotent") end

        -- SetShaderData must accept varied datatypes (number/Color3/Vector3/CFrame/string/bool).
        cube:SetShaderData(shader, "_TestFloat", 0.5)
        cube:SetShaderData(shader, "_TestColor", Color3.new(1, 0, 0))
        cube:SetShaderData(shader, "_TestVec",   Vector3.new(1, 2, 3))
        cube:SetShaderData(shader, "_TestCFrame", CFrame.new(Vector3.new(0, 1, 0)))
        cube:SetShaderData(shader, "_TestString", "tag")
        cube:SetShaderData(shader, "_TestBool", true)

        -- A bad property name shouldn't error (Unity ignores unknown properties).
        -- But missing property name must error.
        local okMissing = pcall(function() cube:SetShaderData(shader, "", 1) end)
        if okMissing then error("SetShaderData with empty name must error") end

        if not cube:RemoveShader(shader) then error("RemoveShader should return true") end
        if #cube:ListShaders() ~= 0 then error("RemoveShader did not detach") end
        if cube:RemoveShader(shader) ~= false then error("RemoveShader on missing should be false") end
    end

    -- Cloning a Shadable carries shadow flags + shader/material lists across.
    local okLoad2, shader2 = pcall(function() return AssetService:GetShader("Default") end)
    if okLoad2 and shader2 then
        cube:AddShader(shader2)
        cube.CastShadow = false
        local clone = cube:Clone()
        if clone.CastShadow ~= false then error("Clone should preserve CastShadow") end
        if #clone:ListShaders() ~= 1 then error("Clone should preserve attached shaders") end
    end

    -- Hold the shaded cube briefly so attached shaders/materials are visible.
    return 3
end

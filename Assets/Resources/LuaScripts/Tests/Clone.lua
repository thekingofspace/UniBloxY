return function()
    -- Build a small subtree rooted at a clonable Folder, with a clonable
    -- BasePart child carrying its own properties and attributes.
    local root = Instance.new("Folder", game)
    root.Name = "OriginalRoot"
    root:SetAttribute("Tag", "alpha")

    local cube = Instance.new("BasePart", root)
    cube.Name = "OriginalCube"
    cube.Render = true
    cube.Size = Vector3.new(2, 3, 4)
    cube.CFrame = CFrame.new(Vector3.new(5, 6, 7))
    cube.CastShadow = false
    cube.ReceiveShadow = false
    cube:SetAttribute("Color", "blue")

    -- Plain Lua-side fields stored on the instance table should also clone over.
    cube.CustomFlag = 42

    local nested = Instance.new("Folder", cube)
    nested.Name = "Nested"

    -- Clone the root and verify the entire subtree was duplicated.
    local copy = root:Clone()
    if copy == root then error("Clone returned the same instance") end
    if copy.ClassName ~= "Folder" then error("Clone should preserve ClassName") end
    if copy.Name ~= "OriginalRoot" then error("Clone should preserve Name") end
    if copy:GetAttribute("Tag") ~= "alpha" then error("Clone should copy attributes") end

    local children = copy:GetChildren()
    if #children ~= 1 then error("Clone should have 1 child, got " .. tostring(#children)) end

    local cubeCopy = children[1]
    if cubeCopy.ClassName ~= "BasePart" then error("Cloned child should be a BasePart") end
    if cubeCopy.Name ~= "OriginalCube" then error("Cloned child should preserve Name") end
    if cubeCopy.Size ~= Vector3.new(2, 3, 4) then error("Cloned cube Size mismatch") end
    if cubeCopy.CFrame ~= CFrame.new(Vector3.new(5, 6, 7)) then error("Cloned cube CFrame mismatch") end
    if cubeCopy.CastShadow ~= false then error("Cloned cube should preserve CastShadow") end
    if cubeCopy.ReceiveShadow ~= false then error("Cloned cube should preserve ReceiveShadow") end
    if cubeCopy:GetAttribute("Color") ~= "blue" then error("Cloned cube should copy attributes") end
    if cubeCopy.CustomFlag ~= 42 then error("Cloned cube should preserve user-set Lua fields") end

    local nestedChildren = cubeCopy:GetChildren()
    if #nestedChildren ~= 1 then error("Nested child should be cloned") end
    if nestedChildren[1].Name ~= "Nested" then error("Nested clone Name mismatch") end

    -- Mutating the clone must not affect the original.
    cubeCopy.Size = Vector3.new(99, 99, 99)
    if cube.Size == cubeCopy.Size then error("Clone aliases source — properties should be independent") end

    -- Camera (non-clonable) should refuse a Clone() call entirely.
    local cam = game.CurrentCamera
    if cam ~= nil then
        local ok = pcall(function() cam:Clone() end)
        if ok then error("Camera should not be cloneable") end
    end

    -- Instances without a parent in the cloned subtree (the clone has no parent
    -- yet) shouldn't have leaked attributes between source/copy.
    cube:SetAttribute("Tag", "beta")
    if cubeCopy:GetAttribute("Color") ~= "blue" then
        error("Source attribute changes should not bleed into the clone")
    end

    -- Park the clone next to the source so both are visible side by side.
    copy.Parent = game
    cubeCopy.CFrame = CFrame.new(Vector3.new(20, 6, 7))

    return 3
end

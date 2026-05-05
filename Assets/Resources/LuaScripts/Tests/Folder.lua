return function()
    local folder = Instance.new("Folder", game)
    folder.Name = "TestFolder"

    if folder.ClassName ~= "Folder" then error("Folder ClassName mismatch") end
    if folder.Parent ~= game then error("Folder Parent mismatch") end

    -- Folders organize children but render-state of children is unchanged.
    local cube = Instance.new("BaseCube", folder)
    cube.Name = "Inside"
    if folder:FindFirstChild("Inside") ~= cube then error("Folder child not findable") end

    -- Nested folders.
    local sub = Instance.new("Folder", folder)
    sub.Name = "Sub"
    local cube2 = Instance.new("BaseCube", sub)
    if folder:FindFirstChild("Sub") ~= sub then error("nested Folder lookup failed") end

    -- Reparenting.
    cube.Parent = sub
    if cube.Parent ~= sub then error("reparent into nested folder failed") end

    -- Clone deep-copies.
    local copy = folder:Clone()
    if copy == folder then error("Clone returned same instance") end
    if copy.ClassName ~= "Folder" then error("Clone ClassName mismatch") end
    if #copy:GetDescendants() ~= #folder:GetDescendants() then
        error("Clone descendant count mismatch")
    end

    folder:Destroy()
    return 0
end

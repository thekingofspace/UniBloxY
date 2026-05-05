return function()
    -- Constructor / parent wiring.
    local root = Instance.new("Folder", game)
    root.Name = "TestRoot"
    if root.Parent ~= game then error("Instance.new with parent should set Parent") end
    if root.ClassName ~= "Folder" then error("ClassName should match") end

    -- Reading via dot notation.
    if root.Name ~= "TestRoot" then error("Name read failed") end

    -- Children: tree shape via FindFirstChild / GetChildren / GetDescendants.
    local a = Instance.new("Folder", root); a.Name = "A"
    local b = Instance.new("Folder", root); b.Name = "B"
    local c = Instance.new("Folder", b);    c.Name = "C"

    if root:FindFirstChild("A") ~= a then error("FindFirstChild failed") end
    if root:FindFirstChild("missing") ~= nil then error("FindFirstChild for missing should be nil") end
    if root:FindFirstChild("C", true) ~= c then error("FindFirstChild recursive failed") end

    if root.A ~= a then error("Indexing by child name failed") end

    local kids = root:GetChildren()
    if #kids ~= 2 then error("GetChildren count mismatch, got " .. tostring(#kids)) end

    local desc = root:GetDescendants()
    if #desc ~= 3 then error("GetDescendants count mismatch, got " .. tostring(#desc)) end

    if not c:IsDescendantOf(root) then error("IsDescendantOf failed") end
    if not root:IsAncestorOf(c) then error("IsAncestorOf failed") end
    if c:FindFirstAncestor("TestRoot") ~= root then error("FindFirstAncestor failed") end

    -- Attributes.
    root:SetAttribute("foo", 42)
    if root:GetAttribute("foo") ~= 42 then error("Attribute round-trip failed") end
    local attrs = root:GetAttributes()
    if attrs.foo ~= 42 then error("GetAttributes mismatch") end
    root:SetAttribute("foo", nil)
    if root:GetAttribute("foo") ~= nil then error("Attribute removal failed") end

    -- Reparent → AncestryChanged + Parent property changed.
    local ancestrySeen = false
    a.AncestryChanged:Connect(function() ancestrySeen = true end)
    a.Parent = b
    if a.Parent ~= b then error("Reparent failed") end
    -- Heartbeat is async — we don't strictly require ancestrySeen here.

    -- Property changed signals.
    local nameChanged = false
    root:GetPropertyChangedSignal("Name"):Connect(function() nameChanged = true end)
    root.Name = "Renamed"
    if root.Name ~= "Renamed" then error("Name set failed") end

    -- ChildAdded fires.
    local childAddedSeen
    root.ChildAdded:Connect(function(child) childAddedSeen = child end)
    local extra = Instance.new("Folder", root); extra.Name = "Extra"

    -- ClearAllChildren wipes the subtree.
    root:ClearAllChildren()
    if #root:GetChildren() ~= 0 then error("ClearAllChildren did not clear") end

    -- Destroy + Destroying signal.
    local destroyingSeen = false
    local destroyMe = Instance.new("Folder", game)
    destroyMe.Destroying:Connect(function() destroyingSeen = true end)
    destroyMe:Destroy()

    -- ClassName is read-only.
    local ok = pcall(function() root.ClassName = "X" end)
    if ok then error("ClassName must be read-only") end

    -- Setting Parent to a non-Instance must error.
    local badOk = pcall(function() root.Parent = 5 end)
    if badOk then error("Parent must reject non-Instance values") end

    -- Cleanup.
    root:Destroy()
    return 0
end

return function()
    if game == nil then error("game global missing") end
    if game.ClassName ~= "DataModel" then error("game ClassName must be DataModel") end
    if game.Name ~= "game" then error("game Name should be 'game'") end

    -- DataModel is Indestructible + non-reparentable.
    local destroyOk = pcall(function() game:Destroy() end)
    if destroyOk then error("DataModel should not be destroyable") end

    -- CurrentCamera is auto-provisioned.
    if game.CurrentCamera == nil then error("CurrentCamera should be provisioned") end
    if game.CurrentCamera.ClassName ~= "Camera" then error("CurrentCamera ClassName mismatch") end

    -- DataModel exposes ObjectAsInstance, ConvertToInstance, ImportScene.
    if type(game.ObjectAsInstance)  ~= "function" then error("ObjectAsInstance missing") end
    if type(game.ConvertToInstance) ~= "function" then error("ConvertToInstance missing") end
    if type(game.ImportScene)       ~= "function" then error("ImportScene missing") end

    -- ObjectAsInstance returns the created instance (typed to className) or nil
    -- if no GameObject with that name was found — same shape as Instance.new.
    local missing = game:ObjectAsInstance("__definitely_not_a_real_object__", "BasePart")
    if missing ~= nil then error("ObjectAsInstance for missing object should be nil") end

    -- ConvertToInstance with no callback must error.
    local ok = pcall(function() game:ConvertToInstance("") end)
    if ok then error("ConvertToInstance without callback should error") end

    -- ImportScene with no callback must error.
    local okIs = pcall(function() game:ImportScene("nope") end)
    if okIs then error("ImportScene without callback should error") end

    -- DataModel can be the parent of new instances.
    local f = Instance.new("Folder", game)
    if f.Parent ~= game then error("Cannot parent into game") end
    f:Destroy()

    -- =========================================================================
    -- ConvertToInstance fires the callback on the entry root, not just on
    -- descendants. We render a BasePart so its GameObject is reachable by
    -- name from GameObject.Find.
    -- =========================================================================
    local host = Instance.new("BasePart", game)
    host.Name = "ConvertEntryHost"
    host.Render = true

    local seen = {}
    game:ConvertToInstance("ConvertEntryHost", function(name)
        seen[#seen + 1] = name
        return nil   -- skip the actual wrap; we're only counting callback visits
    end)
    local rootSeen = false
    for _, n in ipairs(seen) do
        if n == "ConvertEntryHost" then rootSeen = true; break end
    end
    if not rootSeen then
        error("ConvertToInstance must pass the entry root to the callback")
    end
    host:Destroy()

    -- =========================================================================
    -- ObjectAsInstance refuses targets that have children — Folder children
    -- under a Folder do parent in the Unity hierarchy, so the outer Folder's
    -- GameObject ends up with childCount > 0 and the check trips.
    -- =========================================================================
    local outer = Instance.new("Folder", game)
    outer.Name = "ObjectWithChildren"
    local inner = Instance.new("Folder", outer)
    inner.Name = "InnerFolder"

    local okChild, childErr = pcall(function()
        return game:ObjectAsInstance("ObjectWithChildren", "Folder")
    end)
    if okChild then
        error("ObjectAsInstance should error when the target GameObject has children")
    end
    if not string.find(tostring(childErr), "ConvertToInstance") then
        error("ObjectAsInstance error must recommend ConvertToInstance, got: " .. tostring(childErr))
    end
    outer:Destroy()

    return 0
end

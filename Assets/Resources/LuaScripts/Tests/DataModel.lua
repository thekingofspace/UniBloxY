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

    -- DataModel exposes ObjectAsInstance and ConvertToInstance.
    if type(game.ObjectAsInstance)  ~= "function" then error("ObjectAsInstance missing") end
    if type(game.ConvertToInstance) ~= "function" then error("ConvertToInstance missing") end

    -- ObjectAsInstance with a missing scene object should return nil rather than error.
    local maybeNil = game:ObjectAsInstance("__definitely_not_a_real_object__", "BaseCube")
    if maybeNil ~= nil then error("ObjectAsInstance for missing object should be nil") end

    -- ConvertToInstance with no callback must error.
    local ok = pcall(function() game:ConvertToInstance("") end)
    if ok then error("ConvertToInstance without callback should error") end

    -- DataModel can be the parent of new instances.
    local f = Instance.new("Folder", game)
    if f.Parent ~= game then error("Cannot parent into game") end
    f:Destroy()

    return 0
end

return function()
    local cam = game.CurrentCamera
    if cam == nil then error("game.CurrentCamera missing") end
    if cam.ClassName ~= "Camera" then error("CurrentCamera ClassName mismatch") end

    -- CFrame round-trip.
    if typeof(cam.CFrame) ~= "CFrame" then error("Camera.CFrame default missing") end
    cam.CFrame = CFrame.new(Vector3.new(1, 5, -10))
    if cam.CFrame ~= CFrame.new(Vector3.new(1, 5, -10)) then error("CFrame round-trip failed") end

    -- FOV.
    if type(cam.FOV) ~= "number" then error("Camera.FOV missing") end
    cam.FOV = 75
    if cam.FOV ~= 75 then error("FOV round-trip failed") end

    -- Bad types rejected.
    local ok = pcall(function() cam.FOV = "wide" end)
    if ok then error("FOV = string must error") end
    local ok2 = pcall(function() cam.CFrame = 5 end)
    if ok2 then error("CFrame = number must error") end

    -- Aspect / IsFullScreen / FullScreenMode read-only-ish access.
    if type(cam.Aspect) ~= "number" then error("Aspect missing") end
    if type(cam.IsFullScreen) ~= "boolean" then error("IsFullScreen missing") end
    if type(cam.FullScreenMode) ~= "string" then error("FullScreenMode missing") end

    -- Methods.
    if typeof(cam:GetScreenSize()) ~= "Vector2" then error("GetScreenSize must return Vector2") end
    if typeof(cam:GetWindowSize()) ~= "Vector2" then error("GetWindowSize must return Vector2") end
    if typeof(cam:GetViewSize(10)) ~= "Vector2" then error("GetViewSize must return Vector2") end

    -- WindowResized signal exists.
    if type(cam.WindowResized) ~= "table" or type(cam.WindowResized.Connect) ~= "function" then
        error("WindowResized signal missing")
    end

    -- Camera is Indestructible + Reparentable=false + not clonable.
    local destroyOk = pcall(function() cam:Destroy() end)
    if destroyOk then error("Camera should not be destroyable") end

    local cloneOk = pcall(function() cam:Clone() end)
    if cloneOk then error("Camera should not be cloneable") end

    -- Hold the camera in the new pose so the change is visible.
    return 3
end

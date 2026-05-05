return function()
    if type(InputService) ~= "table" then error("InputService global missing") end

    if type(InputService.Input) ~= "table" then error("InputService.Input signal missing") end
    if type(InputService.Input.Connect) ~= "function" then error("Input is not a Signal") end

    if type(InputService.IsKeyDown) ~= "function" then error("IsKeyDown missing") end
    if type(InputService.GetMouse)  ~= "function" then error("GetMouse missing") end

    -- IsKeyDown for an unrealistic key returns false.
    if InputService.IsKeyDown("ZZZNotARealKey") ~= false then
        error("IsKeyDown for fake key should be false")
    end

    -- Mouse table.
    local mouse = InputService:GetMouse()
    if type(mouse) ~= "table" then error("GetMouse must return a table") end

    for _, sig in ipairs({ "Clicked", "Moved", "ButtonDown", "ButtonUp", "Scrolled" }) do
        if type(mouse[sig]) ~= "table" or type(mouse[sig].Connect) ~= "function" then
            error("Mouse." .. sig .. " missing")
        end
    end

    if type(mouse.IsButtonDown) ~= "function" then error("Mouse:IsButtonDown missing") end
    if type(mouse.SetLocked)    ~= "function" then error("Mouse:SetLocked missing") end
    if type(mouse.SetVisible)   ~= "function" then error("Mouse:SetVisible missing") end

    -- Position is exposed via metatable __index.
    if typeof(mouse.Position) ~= "Vector2" then
        error("Mouse.Position must be a Vector2")
    end

    -- IsButtonDown for a fake button returns false (does not error).
    if mouse:IsButtonDown("FakeButton") ~= false then
        error("IsButtonDown for fake button should be false")
    end

    -- SetLocked/SetVisible accept booleans without erroring.
    mouse:SetVisible(true)
    mouse:SetLocked(false)

    return 0
end

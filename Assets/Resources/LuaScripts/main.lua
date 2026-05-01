local task = require("task")
local mouse = InputService.GetMouse()

print("Environment: " .. RunService.GetEnvironment())

task.spawn(function()
    print("hello from lua")
end)
---@type number
local elapsed = 0

---@param dt number
RunService.Heartbeat:Connect(function(dt)
    elapsed = elapsed + dt
    if elapsed >= 1 then
        elapsed = 0
        
    end
end)

InputService.Input:Connect(function(input, state)
    print(input.KeyCode,state)
end)

RunService.BindToClose(function()
    print("ran")
end)

task.delay(2, function()
    print("Ran")
end)

task.wait(2)

RunService.Close()
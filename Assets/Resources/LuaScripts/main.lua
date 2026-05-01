local task = require("task")
local mouse = InputService.GetMouse()

print("Environment: " .. RunService.GetEnvironment())

task.spawn(function()
    print("hello from lua")
end)
---@type number
local elapsed = 0
RunService.Heartbeat:Connect(function(dt)
    elapsed = elapsed + dt
    if elapsed >= 1 then
        elapsed = 0
        print("tick")
    end
end)

InputService.Input:Connect(function(input, state)
    print(input.KeyCode)
    print(state)
end)

task.wait(2)
print("ran")

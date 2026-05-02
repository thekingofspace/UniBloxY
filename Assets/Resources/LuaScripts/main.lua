local camera = game.CurrentCamera
local SPEED = 10

local moveX, moveY, moveZ = 0, 0, 0

local FOV = 70
local FOV_SPEED = 5

local sensitivity = 0.002

local yaw = 0
local pitch = 0 



local keyAxis = {
    W = { axis = "z", dir = 1 },
    S = { axis = "z", dir = -1 },
    A = { axis = "x", dir = -1 },
    D = { axis = "x", dir = 1 },
    E = { axis = "y", dir = 1 },
    Q = { axis = "y", dir = -1 },
    R = { axis = "fov", dir = 1 },
    F = { axis = "fov", dir = -1 },
}

InputService.Input:Connect(function(input, state)
    local map = keyAxis[input.KeyCode]
    if not map then return end

    local delta = state == "Begin" and map.dir or -map.dir

    if map.axis == "x" then
        moveX = moveX + delta
    elseif map.axis == "y" then
        moveY = moveY + delta
    elseif map.axis == "z" then
        moveZ = moveZ + delta
    elseif map.axis == "fov" then
        FOV = math.max(20, math.min(120, FOV + delta * FOV_SPEED))
        camera.FOV = FOV
    end
end)

local mouse = InputService.GetMouse()

mouse.Moved:Connect(function(pos, delta)
    yaw = yaw - delta.X * sensitivity
    pitch = math.max(-1.5, math.min(1.5, pitch - delta.Y * sensitivity))
end)

local highestFPS = 0

RunService.Heartbeat:Connect(function(dt)
    local fps = RunService.FPS
    if fps > highestFPS then
        highestFPS = fps
        print("New highest FPS:", highestFPS)
    end

    local pos = camera.CFrame.Position

    local baseCFrame =
        CFrame.new(pos.X, pos.Y, pos.Z) *
        CFrame.Angles(0, yaw, 0) *
        CFrame.Angles(pitch, 0, 0)

    camera.CFrame = baseCFrame

    if moveX ~= 0 or moveY ~= 0 or moveZ ~= 0 then
        local len = math.sqrt(moveX * moveX + moveY * moveY + moveZ * moveZ)
        local step = SPEED * dt / len

        camera.CFrame = camera.CFrame * CFrame.new(
            moveX * step,
            moveY * step,
            moveZ * step
        )
    end
end)
camera.FOV = FOV

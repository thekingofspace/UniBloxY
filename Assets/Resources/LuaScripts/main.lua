local task = require ("task")
task.spawn(function()
    print("ran1")
end)

---@async
task.defer(function()
    print("ran2")
    task.wait(0.2)
    print("ran3")
end)

local Camera = Unity.GetCamera()

local startCF = Camera.CFrame
local pos = { x = startCF.X, y = startCF.Y, z = startCF.Z }
local baseY = startCF.Y
---@type number, number
local yaw, pitch = 0, 0

local held = {}
UserInput.OnInput(function(input, state)
    if state == "Input" then
        held[input.KeyName] = true
    else
        held[input.KeyName] = nil
    end
end)

local mouse = UserInput.GetMouse()
---@type number
local sensitivity = 0.004
---@type number
local pitchLimit = 1.4
mouse.OnMove(function(delta)
    yaw = yaw + delta.X * sensitivity
    pitch = pitch - delta.Y * sensitivity
    if pitch > pitchLimit then pitch = pitchLimit end
    if pitch < -pitchLimit then pitch = -pitchLimit end
end)

local moveSpeed = 6
local runMultiplier = 1.8
local bobFreq = 10
local bobAmpY = 0.12
local bobAmpX = 0.06
local runBobFreqMul = 1.6
local runBobAmpMul = 1.8
---@type number
local bobTime = 0

local jumpVelocity = 5.5
local gravity = 18
local verticalSpeed = 4
---@type number
local vy = 0
---@type boolean
local grounded = true

Unity.BindToHeartbeat(0, "WASDWalk", function(dt)
    ---@type number, number
    local fx, fz = 0, 0
    if held.W then fz = fz + 1 end
    if held.S then fz = fz - 1 end
    if held.D then fx = fx + 1 end
    if held.A then fx = fx - 1 end

    local running = held.LeftShift or held.RightShift
    local speed = moveSpeed * (running and runMultiplier or 1)
    local freq = bobFreq * (running and runBobFreqMul or 1)
    local ampY = bobAmpY * (running and runBobAmpMul or 1)
    local ampX = bobAmpX * (running and runBobAmpMul or 1)

    local moving = (fx ~= 0 or fz ~= 0)

    if moving then
        local len = math.sqrt(fx * fx + fz * fz)
        fx, fz = fx / len, fz / len
        local sinY = math.sin(yaw)
        local cosY = math.cos(yaw)
        local wx = sinY * fz + cosY * fx
        local wz = cosY * fz - sinY * fx

        pos.x = pos.x + wx * speed * dt
        pos.z = pos.z + wz * speed * dt
        bobTime = bobTime + dt
    end

    ---@type number
    local vertical = 0
    if held.E then vertical = vertical + 1 end
    if held.Q then vertical = vertical - 1 end
    if vertical ~= 0 then
        baseY = baseY + vertical * verticalSpeed * dt
        if grounded then pos.y = baseY end
    end

    if grounded and held.Space then
        vy = jumpVelocity
        grounded = false
    end

    if not grounded then
        vy = vy - gravity * dt
        pos.y = pos.y + vy * dt
        if pos.y <= baseY then
            pos.y = baseY
            vy = 0
            grounded = true
        end
    end

    ---@type number, number
    local bobX, bobY = 0, 0
    if moving and grounded then
        bobY = math.abs(math.sin(bobTime * freq)) * ampY
        bobX = math.cos(bobTime * freq * 0.5) * ampX
    end

    local sinY = math.sin(yaw)
    local cosY = math.cos(yaw)
    local rightX = cosY
    local rightZ = -sinY

    local cx = pos.x + rightX * bobX
    local cy = pos.y + bobY
    local cz = pos.z + rightZ * bobX

    Camera.CFrame = CFrame.New(cx, cy, cz) * CFrame.Angles(pitch, yaw, 0)
end)

UserInput.SetMouseLocked(true)
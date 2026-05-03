local outerGroup = Instance.New("RenderGroup", game)
outerGroup.Name = "OuterGroup"

local center = Instance.New("BaseCube", outerGroup)
center.Name = "Center"
center.Size = Vector3.new(1.5, 1.5, 1.5)
center.CFrame = CFrame.new(0, 5, 0)

local innerGroup = Instance.New("RenderGroup", center)
innerGroup.Name = "InnerGroup"

local overrideGroup = Instance.New("RenderGroup", center)
overrideGroup.Name = "OverrideGroup"
overrideGroup.OverrideParent = true
overrideGroup.Render = false

local radius = 4
for i = 1, 7 do
    local angle = (i - 1) * (2 * math.pi / 7)
    local x = math.cos(angle) * radius
    local z = math.sin(angle) * radius

    local parent = (i % 2 == 1) and innerGroup or center
    local cube = Instance.New("BaseCube", parent)
    cube.Name = "Ring" .. i .. "_" .. (parent == innerGroup and "Inner" or "Outer")
    cube.Size = Vector3.new(1, 1, 1)
    cube.CFrame = CFrame.new(x, 5, z)
end

-- cube controlled by override group
local overrideCube = Instance.New("BaseCube", overrideGroup)
overrideCube.Name = "OverrideCube"
overrideCube.Size = Vector3.new(1, 1, 1)
overrideCube.CFrame = CFrame.new(0, 7, 0)

outerGroup.Render = true
innerGroup.Render = true

local spinAngle = 0
local elapsed = 0
local lastStage = -1

RunService.Heartbeat:Connect(function(dt)
    spinAngle = (spinAngle + dt * 60) % 360
    center.CFrame = CFrame.new(0, 5, 0, 0, spinAngle, 0)

    elapsed = elapsed + dt
    local phase = elapsed % 10
    local stage
    if phase < 3 then stage = 0
    elseif phase < 5 then stage = 1
    elseif phase < 7 then stage = 2
    else stage = 3 end

    if stage ~= lastStage then
        lastStage = stage

        overrideGroup.Render = false

        if stage == 0 then
            outerGroup.Render = true
            innerGroup.Render = true
            print("[stage 0] both on")

        elseif stage == 1 then
            innerGroup.Render = false
            print("[stage 1] inner off")

        elseif stage == 2 then
            outerGroup.Render = false
            innerGroup.Render = false
            overrideGroup.Render = true
            print("[stage 2] override active")

        else
            outerGroup.Render = true
            innerGroup.Render = true
            print("[stage 3] both on again")
        end
    end
end)
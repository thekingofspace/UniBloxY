local outerGroup = Instance.New("RenderGroup", game)
outerGroup.Name = "OuterGroup"

local center = Instance.New("BaseCube", outerGroup)
center.Name = "Center"
center.Size = Vector3.new(1.5, 1.5, 1.5)
center.CFrame = CFrame.new(0, 5, 0)

local innerGroup = Instance.New("RenderGroup", center)
innerGroup.Name = "InnerGroup"

local radius = 4
for i = 1, 7 do
    local angle = (i - 1) * (2 * math.pi / 7)
    local x = math.cos(angle) * radius
    local z = math.sin(angle) * radius

    -- alternate cubes between inner group and direct children of center
    local parent = (i % 2 == 1) and innerGroup or center
    local cube = Instance.New("BaseCube", parent)
    cube.Name = "Ring" .. i .. "_" .. (parent == innerGroup and "Inner" or "Outer")
    cube.Size = Vector3.new(1, 1, 1)
    cube.CFrame = CFrame.new(x, 5, z)
end

outerGroup.Render = true
innerGroup.Render = true

local spinAngle = 0
local elapsed = 0
local lastStage = -1

RunService.Heartbeat:Connect(function(dt)
    spinAngle = (spinAngle + dt * 60) % 360
    center.CFrame = CFrame.new(0, 5, 0, 0, spinAngle, 0)

    elapsed = elapsed + dt
    -- 10s loop:
    --   0-3 : both on   (all 7 cubes visible)
    --   3-5 : inner off (only the "Outer" cubes visible)
    --   5-7 : outer off (nothing visible — outer gates inner per AND rule)
    --   7-10: both on   (all visible again)
    local phase = elapsed % 10
    local stage
    if phase < 3 then stage = 0
    elseif phase < 5 then stage = 1
    elseif phase < 7 then stage = 2
    else stage = 3 end

    if stage ~= lastStage then
        lastStage = stage
        if stage == 0 then
            outerGroup.Render = true; innerGroup.Render = true
            print("[stage 0] both on")
        elseif stage == 1 then
            innerGroup.Render = false
            print("[stage 1] inner off — only Outer cubes show")
        elseif stage == 2 then
            outerGroup.Render = false
            print("[stage 2] outer off — nothing shows (gates inner)")
        else
            outerGroup.Render = true; innerGroup.Render = true
            print("[stage 3] both on again")
        end
    end
end)

print(game:ConvertToInstance("", function(name)
    print(name)
end))
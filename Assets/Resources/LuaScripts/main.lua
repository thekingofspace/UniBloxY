local root = Instance.New("BaseCube", game)
root.Name = "Root"
root.Size = Vector3.new(2,2,2)
root.CFrame = CFrame.new(0,0,0)

local mid = Instance.New("BaseCube", root)
mid.Name = "Mid"
mid.Size = Vector3.new(1.5,1.5,1.5)
mid.CFrame = root.CFrame * CFrame.new(0,3,0)

local top = Instance.New("BaseCube", mid)
top.Name = "Top"
top.Size = Vector3.new(1,1,1)
top.CFrame = mid.CFrame * CFrame.new(0,3,0)

local blue = ShaderService.GetShader("Blue")
local cubes = { root, mid, top }
for _, c in ipairs(cubes) do c:AddShader(blue) end

local function setCells(cellsAcross)
    for _, c in ipairs(cubes) do
        c:SetShaderData(blue, "_Density", cellsAcross / c.Size.X)
    end
end

local cellsStart, cellsEnd, shrinkTime = 8, 1, 15
setCells(cellsStart)
print("Shaders on root:", #root:ListShaders())

local t = 0
local phase = 1

root.Render = true
mid.Render = true
top.Render = true

RunService.Heartbeat:Connect(function(dt)
    t = t +dt

    local k = math.min(t / shrinkTime, 1)
    setCells(cellsStart + (cellsEnd - cellsStart) * k)

    if phase == 1 then
        root.CFrame = root.CFrame * CFrame.new(0, 0, dt * 2)
        if t >= 10 then
            phase = 2
        end
    elseif phase == 2 then
        mid.CFrame = mid.CFrame * CFrame.new(dt * 2, 0, 0)

        if t >= 20 then
            phase = 1
            t = 0
        end
    end
end)
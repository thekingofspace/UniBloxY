return function()
    -- Vector3: equality by X/Y/Z, ordering by SquaredMagnitude.
    local a = Vector3.new(1, 2, 3)
    local b = Vector3.new(1, 2, 3)
    local c = Vector3.new(4, 5, 6)
    if not (a == b) then error("Vector3 equality should compare X/Y/Z") end
    if a == c then error("Vector3 inequality failed") end
    if a ~= b then error("Vector3 ~= for equal values failed") end
    if not (a < c) then error("Vector3 < should compare squared magnitude") end
    if not (c > a) then error("Vector3 > should compare squared magnitude") end
    if not (a <= b) then error("Vector3 <= for equal values failed") end
    if not (a >= b) then error("Vector3 >= for equal values failed") end

    -- Vector2 mirrors Vector3.
    local v2a = Vector2.new(3, 4)
    local v2b = Vector2.new(3, 4)
    local v2c = Vector2.new(0, 1)
    if not (v2a == v2b) then error("Vector2 equality failed") end
    if v2a == v2c then error("Vector2 inequality failed") end
    if not (v2c < v2a) then error("Vector2 ordering failed") end

    -- Color3: equality by R/G/B, ordering by luma.
    local red    = Color3.new(1, 0, 0)
    local red2   = Color3.new(1, 0, 0)
    local white  = Color3.new(1, 1, 1)
    if not (red == red2) then error("Color3 equality should compare RGB") end
    if red == white then error("Color3 inequality failed") end
    if not (red < white) then error("Color3 luma ordering failed") end

    -- CFrame: equality and ordering by Position only — rotation is ignored.
    local cf1 = CFrame.new(Vector3.new(1, 2, 3))
    local cf2 = CFrame.new(Vector3.new(1, 2, 3), Vector3.new(45, 90, 0))
    if not (cf1 == cf2) then error("CFrame equality should compare Position only (ignoring rotation)") end

    local cf3 = CFrame.new(Vector3.new(10, 0, 0))
    if not (cf1 < cf3) then error("CFrame ordering should follow position magnitude") end

    -- UDim: equality by Scale/Offset.
    local u1 = UDim.new(0.5, 10)
    local u2 = UDim.new(0.5, 10)
    local u3 = UDim.new(1, 0)
    if not (u1 == u2) then error("UDim equality failed") end
    if u1 == u3 then error("UDim inequality failed") end

    -- UDim2.
    local u2a = UDim2.new(0, 100, 0, 50)
    local u2b = UDim2.new(0, 100, 0, 50)
    local u2c = UDim2.new(1, 0, 1, 0)
    if not (u2a == u2b) then error("UDim2 equality failed") end
    if u2a == u2c then error("UDim2 inequality failed") end

    return 0
end

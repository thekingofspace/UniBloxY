return function()
    local function approx(a, b, eps)
        return math.abs(a - b) <= (eps or 1e-4)
    end

    -- ---- Vector2 ----
    local v2 = Vector2.new(3, 4)
    if v2.X ~= 3 or v2.Y ~= 4 then error("Vector2 components mismatch") end
    if v2.ClassName ~= "Vector2" then error("Vector2 ClassName mismatch") end
    if not approx(v2.Magnitude, 5) then error("Vector2.Magnitude mismatch") end
    if v2.SquaredMagnitude ~= 25 then error("Vector2.SquaredMagnitude mismatch") end
    local unit = v2.Unit
    if not approx(unit.Magnitude, 1) then error("Vector2.Unit should have magnitude 1") end

    if (v2 + Vector2.new(1, 1)) ~= Vector2.new(4, 5) then error("Vector2 + failed") end
    if (v2 - Vector2.new(1, 1)) ~= Vector2.new(2, 3) then error("Vector2 - failed") end
    if (v2 * 2) ~= Vector2.new(6, 8) then error("Vector2 * scalar failed") end
    if (v2 / 2) ~= Vector2.new(1.5, 2) then error("Vector2 / scalar failed") end
    if (-v2) ~= Vector2.new(-3, -4) then error("Vector2 unary minus failed") end

    if v2:Dot(Vector2.new(1, 0)) ~= 3 then error("Vector2:Dot failed") end
    if not approx(v2:Distance(Vector2.new(0, 0)), 5) then error("Vector2:Distance failed") end
    if v2:Min(Vector2.new(2, 5)) ~= Vector2.new(2, 4) then error("Vector2:Min failed") end
    if v2:Max(Vector2.new(2, 5)) ~= Vector2.new(3, 5) then error("Vector2:Max failed") end
    if v2:Abs() ~= Vector2.new(3, 4) then error("Vector2:Abs failed") end
    if Vector2.new(-1.7, 2.3):Floor() ~= Vector2.new(-2, 2) then error("Vector2:Floor failed") end
    if Vector2.new(-1.7, 2.3):Ceil()  ~= Vector2.new(-1, 3) then error("Vector2:Ceil failed") end
    if not v2:FuzzyEq(Vector2.new(3.0001, 4), 0.001) then error("Vector2:FuzzyEq failed") end
    if v2:Lerp(Vector2.new(7, 8), 0.5) ~= Vector2.new(5, 6) then error("Vector2:Lerp failed") end

    if Vector2.zero ~= Vector2.new(0, 0) then error("Vector2.zero mismatch") end
    if Vector2.one  ~= Vector2.new(1, 1) then error("Vector2.one mismatch") end

    -- ---- Vector3 ----
    local v3 = Vector3.new(1, 2, 3)
    if v3.X ~= 1 or v3.Y ~= 2 or v3.Z ~= 3 then error("Vector3 components mismatch") end
    if v3.ClassName ~= "Vector3" then error("Vector3 ClassName mismatch") end
    if not approx(v3.Magnitude, math.sqrt(14)) then error("Vector3.Magnitude mismatch") end

    if v3:Dot(Vector3.new(1, 0, 0)) ~= 1 then error("Vector3:Dot failed") end
    if v3:Cross(Vector3.new(1, 0, 0)) ~= Vector3.new(0, 3, -2) then error("Vector3:Cross failed") end
    if not approx(v3:Distance(Vector3.zero), math.sqrt(14)) then error("Vector3:Distance failed") end

    if (v3 + Vector3.one)        ~= Vector3.new(2, 3, 4) then error("Vector3 + failed") end
    if (v3 * Vector3.new(2,2,2)) ~= Vector3.new(2, 4, 6) then error("Vector3 component mul failed") end

    -- ---- Color3 ----
    local c = Color3.new(0.25, 0.5, 0.75)
    if c.R ~= 0.25 or c.G ~= 0.5 or c.B ~= 0.75 then error("Color3 components mismatch") end
    if c.ClassName ~= "Color3" then error("Color3 ClassName mismatch") end
    if Color3.fromRGB(255, 0, 0) ~= Color3.new(1, 0, 0) then error("Color3.fromRGB failed") end
    if Color3.fromHex("#FF0000")  ~= Color3.new(1, 0, 0) then error("Color3.fromHex failed") end
    if Color3.new(1, 0, 0):ToHex() ~= "#FF0000" then error("Color3:ToHex failed") end
    local hsv = c:ToHSV()
    if type(hsv) ~= "table" or type(hsv.H) ~= "number" then error("Color3:ToHSV missing H") end

    -- ---- UDim / UDim2 ----
    local u = UDim.new(0.5, 10)
    if u.Scale ~= 0.5 or u.Offset ~= 10 then error("UDim components mismatch") end
    if (u + UDim.new(0.5, 10)) ~= UDim.new(1, 20) then error("UDim + failed") end
    if u:Lerp(UDim.new(1, 20), 0.5) ~= UDim.new(0.75, 15) then error("UDim:Lerp failed") end

    local u2 = UDim2.fromScale(0.5, 0.25)
    if u2.X.Scale ~= 0.5 or u2.Y.Scale ~= 0.25 then error("UDim2.fromScale failed") end
    local u2o = UDim2.fromOffset(100, 50)
    if u2o.X.Offset ~= 100 or u2o.Y.Offset ~= 50 then error("UDim2.fromOffset failed") end

    -- ---- CFrame ----
    local cf = CFrame.new(Vector3.new(1, 2, 3))
    if cf.Position ~= Vector3.new(1, 2, 3) then error("CFrame.Position mismatch") end
    if cf.Rotation ~= Vector3.zero then error("CFrame default rotation should be zero") end

    if CFrame.identity.Position ~= Vector3.zero then error("CFrame.identity wrong") end
    if CFrame.fromPosition(Vector3.new(5, 0, 0)).Position ~= Vector3.new(5, 0, 0) then
        error("CFrame.fromPosition mismatch")
    end

    local angled = CFrame.Angles(0, 90, 0)
    if angled.Position ~= Vector3.zero then error("CFrame.Angles position should be zero") end

    local lookAt = CFrame.LookAt(Vector3.zero, Vector3.new(0, 0, 1))
    if typeof(lookAt) ~= "CFrame" then error("CFrame.LookAt should return a CFrame") end

    -- CFrame * Vector3 transforms a point.
    local transformed = cf * Vector3.new(0, 0, 0)
    if transformed ~= Vector3.new(1, 2, 3) then error("CFrame * Vector3 failed") end

    -- typeof
    if typeof(v2) ~= "Vector2" then error("typeof(Vector2) failed") end
    if typeof(v3) ~= "Vector3" then error("typeof(Vector3) failed") end
    if typeof(c)  ~= "Color3"  then error("typeof(Color3) failed")  end
    if typeof(u)  ~= "UDim"    then error("typeof(UDim) failed")    end
    if typeof(cf) ~= "CFrame"  then error("typeof(CFrame) failed")  end
    if typeof(1)  ~= "number"  then error("typeof(number) failed")  end
    if typeof("x")~= "string"  then error("typeof(string) failed")  end
    if typeof(nil)~= "nil"     then error("typeof(nil) failed")     end

    return 0
end

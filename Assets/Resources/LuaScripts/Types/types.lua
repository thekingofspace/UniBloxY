---@meta

-- =============================================================================
-- Vector2
-- =============================================================================

---@class Vector2
---@field ClassName "Vector2"
---@field X number
---@field Y number
---@field Magnitude number
---@field SquaredMagnitude number
---@field Unit Vector2
---@operator add(Vector2): Vector2
---@operator sub(Vector2): Vector2
---@operator mul(Vector2): Vector2
---@operator mul(number): Vector2
---@operator div(Vector2): Vector2
---@operator div(number): Vector2
---@operator unm: Vector2
local Vector2Inst = {}

---@param other Vector2
---@return number
function Vector2Inst:Dot(other) end
---@param other Vector2
---@return number
function Vector2Inst:Cross(other) end
---@param other Vector2
---@return number
function Vector2Inst:Distance(other) end
---@param other Vector2
---@return number  -- radians
function Vector2Inst:Angle(other) end
---@return Vector2
function Vector2Inst:Abs() end
---@return Vector2
function Vector2Inst:Floor() end
---@return Vector2
function Vector2Inst:Ceil() end
---@return Vector2
function Vector2Inst:Sign() end
---@param other Vector2
---@return Vector2
function Vector2Inst:Min(other) end
---@param other Vector2
---@return Vector2
function Vector2Inst:Max(other) end
---@param other Vector2
---@param epsilon number
---@return boolean
function Vector2Inst:FuzzyEq(other, epsilon) end
---@param other Vector2
---@param t number
---@return Vector2
function Vector2Inst:Lerp(other, t) end

---@class Vector2lib
---@field zero Vector2
---@field one Vector2
---@field xAxis Vector2
---@field yAxis Vector2
Vector2 = {}

---@param x number
---@param y number
---@return Vector2
function Vector2.new(x, y) end

-- =============================================================================
-- Vector3
-- =============================================================================

---@class Vector3
---@field ClassName "Vector3"
---@field X number
---@field Y number
---@field Z number
---@field Magnitude number
---@field SquaredMagnitude number
---@field Unit Vector3
---@operator add(Vector3): Vector3
---@operator sub(Vector3): Vector3
---@operator mul(Vector3): Vector3
---@operator mul(number): Vector3
---@operator div(Vector3): Vector3
---@operator div(number): Vector3
---@operator unm: Vector3
local Vector3Inst = {}

---@param other Vector3
---@return number
function Vector3Inst:Dot(other) end
---@param other Vector3
---@return Vector3
function Vector3Inst:Cross(other) end
---@param other Vector3
---@return number
function Vector3Inst:Distance(other) end
---@param other Vector3
---@return number  -- radians
function Vector3Inst:Angle(other) end
---@return Vector3
function Vector3Inst:Abs() end
---@return Vector3
function Vector3Inst:Floor() end
---@return Vector3
function Vector3Inst:Ceil() end
---@return Vector3
function Vector3Inst:Sign() end
---@param other Vector3
---@return Vector3
function Vector3Inst:Min(other) end
---@param other Vector3
---@return Vector3
function Vector3Inst:Max(other) end
---@param other Vector3
---@param epsilon number
---@return boolean
function Vector3Inst:FuzzyEq(other, epsilon) end
---@param other Vector3
---@param t number
---@return Vector3
function Vector3Inst:Lerp(other, t) end

---@class Vector3lib
---@field zero Vector3
---@field one Vector3
---@field xAxis Vector3
---@field yAxis Vector3
---@field zAxis Vector3
Vector3 = {}

---@param x number
---@param y number
---@param z number
---@return Vector3
function Vector3.new(x, y, z) end

-- =============================================================================
-- Color3
-- =============================================================================

---@class Color3
---@field ClassName "Color3"
---@field R number
---@field G number
---@field B number
---@operator add(Color3): Color3
---@operator sub(Color3): Color3
---@operator mul(Color3): Color3
---@operator mul(number): Color3
local Color3Inst = {}

---@param other Color3
---@param t number
---@return Color3
function Color3Inst:Lerp(other, t) end

---@return string  -- "#RRGGBB"
function Color3Inst:ToHex() end

---@return { H:number, S:number, V:number }
function Color3Inst:ToHSV() end

---@class Color3lib
Color3 = {}

---Components are 0-1 floats.
---@param r number
---@param g number
---@param b number
---@return Color3
function Color3.new(r, g, b) end

---Components are 0-255 integers.
---@param r integer
---@param g integer
---@param b integer
---@return Color3
function Color3.fromRGB(r, g, b) end

---@param h number
---@param s number
---@param v number
---@return Color3
function Color3.fromHSV(h, s, v) end

---@param hex string  -- "#RRGGBB" or "RRGGBB"
---@return Color3
function Color3.fromHex(hex) end

-- =============================================================================
-- UDim
-- =============================================================================

---@class UDim
---@field ClassName "UDim"
---@field Scale number
---@field Offset number
---@operator add(UDim): UDim
---@operator sub(UDim): UDim
---@operator mul(number): UDim
---@operator div(number): UDim
---@operator unm: UDim
local UDimInst = {}

---@param other UDim
---@param t number
---@return UDim
function UDimInst:Lerp(other, t) end

---@class UDimlib
UDim = {}

---@param scale number
---@param offset number
---@return UDim
function UDim.new(scale, offset) end

-- =============================================================================
-- UDim2
-- =============================================================================

---@class UDim2
---@field ClassName "UDim2"
---@field X UDim
---@field Y UDim
---@operator add(UDim2): UDim2
---@operator sub(UDim2): UDim2
---@operator mul(number): UDim2
---@operator div(number): UDim2
---@operator unm: UDim2
local UDim2Inst = {}

---@param other UDim2
---@param t number
---@return UDim2
function UDim2Inst:Lerp(other, t) end

---@class UDim2lib
UDim2 = {}

---@param xScale number
---@param xOffset number
---@param yScale number
---@param yOffset number
---@return UDim2
function UDim2.new(xScale, xOffset, yScale, yOffset) end

---@param x number
---@param y number
---@return UDim2
function UDim2.fromScale(x, y) end

---@param x number
---@param y number
---@return UDim2
function UDim2.fromOffset(x, y) end

-- =============================================================================
-- CFrame
-- =============================================================================

---@class CFrame
---@field ClassName "CFrame"
---@field Position Vector3
---@field Rotation Vector3
---@field Angles Vector3
---@field LookVector Vector3
---@field RightVector Vector3
---@field UpVector Vector3
---@operator mul(CFrame): CFrame
---@operator mul(Vector3): Vector3
---@operator add(Vector3): CFrame
---@operator sub(Vector3): CFrame
local CFrameInst = {}

---@param other CFrame
---@param t number
---@return CFrame
function CFrameInst:Lerp(other, t) end

---@return CFrame
function CFrameInst:Inverse() end

---@param cf CFrame
---@return CFrame
function CFrameInst:ToWorldSpace(cf) end

---@param cf CFrame
---@return CFrame
function CFrameInst:ToObjectSpace(cf) end

---@param v Vector3
---@return Vector3
function CFrameInst:PointToWorldSpace(v) end

---@param v Vector3
---@return Vector3
function CFrameInst:PointToObjectSpace(v) end

---@param v Vector3
---@return Vector3
function CFrameInst:VectorToWorldSpace(v) end

---@param v Vector3
---@return Vector3
function CFrameInst:VectorToObjectSpace(v) end

---@class CFramelib
---@field identity CFrame
CFrame = {}

---@overload fun(x:number, y:number, z:number):CFrame
---@overload fun(px:number, py:number, pz:number, rx:number, ry:number, rz:number):CFrame
---@overload fun(position:Vector3):CFrame
---@overload fun(position:Vector3, rotation:Vector3):CFrame
---@return CFrame
function CFrame.new(...) end

---@param position Vector3
---@return CFrame
function CFrame.fromPosition(position) end

---@param position Vector3
---@param eulerRotation Vector3
---@return CFrame
function CFrame.fromEulerAngles(position, eulerRotation) end

---@param rx number
---@param ry number
---@param rz number
---@return CFrame
function CFrame.Angles(rx, ry, rz) end

---@param axis Vector3
---@param angle number  -- radians
---@return CFrame
function CFrame.fromAxisAngle(axis, angle) end

---@param eye Vector3
---@param target Vector3
---@param up Vector3?
---@return CFrame
function CFrame.LookAt(eye, target, up) end

-- =============================================================================
-- typeof
-- =============================================================================

---@param value any
---@return string
function typeof(value) end

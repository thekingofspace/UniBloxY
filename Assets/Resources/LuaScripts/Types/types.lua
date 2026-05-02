---@meta

-- =============================================================================
-- Vector2
-- =============================================================================

---@class Vector2
---@field ClassName "Vector2"
---@field X number
---@field Y number
---@operator add(Vector2): Vector2
---@operator sub(Vector2): Vector2
---@operator mul(number): Vector2
---@operator div(number): Vector2
---@operator unm: Vector2

---@class Vector2lib
---@field zero Vector2
---@field one Vector2
Vector2 = {}

---@param x number
---@param y number
---@return Vector2
function Vector2.new(x, y) end

---@param other Vector2
---@param t number
---@return Vector2
function Vector2:Lerp(other, t) end

-- =============================================================================
-- Vector3
-- =============================================================================

---@class Vector3
---@field ClassName "Vector3"
---@field X number
---@field Y number
---@field Z number
---@operator add(Vector3): Vector3
---@operator sub(Vector3): Vector3
---@operator mul(number): Vector3
---@operator div(number): Vector3
---@operator unm: Vector3

---@class Vector3lib
---@field zero Vector3
---@field one Vector3
Vector3 = {}

---@param x number
---@param y number
---@param z number
---@return Vector3
function Vector3.new(x, y, z) end

---@param other Vector3
---@param t number
---@return Vector3
function Vector3:Lerp(other, t) end

-- =============================================================================
-- Color3
-- =============================================================================

---@class Color3
---@field ClassName "Color3"
---@field R number
---@field G number
---@field B number
---@operator mul(number): Color3

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

---@param other Color3
---@param t number
---@return Color3
function Color3:Lerp(other, t) end

-- =============================================================================
-- UDim
-- =============================================================================

---@class UDim
---@field ClassName "UDim"
---@field Scale number
---@field Offset number

---@class UDimlib
UDim = {}

---@param scale number
---@param offset number
---@return UDim
function UDim.new(scale, offset) end

---@param other UDim
---@param t number
---@return UDim
function UDim:Lerp(other, t) end

-- =============================================================================
-- UDim2
-- =============================================================================

---@class UDim2
---@field ClassName "UDim2"
---@field X UDim
---@field Y UDim
---@operator add(UDim2): UDim2
---@operator sub(UDim2): UDim2

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

---@param other UDim2
---@param t number
---@return UDim2
function UDim2:Lerp(other, t) end

-- =============================================================================
-- CFrame
-- =============================================================================

---@class CFrame
---@field ClassName "CFrame"
---@field Position Vector3
---@field Rotation Vector3
---@field Angles Vector3  -- euler angles (alias for Rotation)

---@class CFramelib
CFrame = {}

---@param x number
---@param y number
---@param z number
---@return CFrame
function CFrame.new(x, y, z) end

---@param position Vector3
---@return CFrame
function CFrame.fromPosition(position) end

---@param position Vector3
---@param eulerRotation Vector3
---@return CFrame
function CFrame.fromEulerAngles(position, eulerRotation) end

---Creates a rotation-only CFrame from Euler angles (position is zero).
---@param rx number
---@param ry number
---@param rz number
---@return CFrame
function CFrame.Angles(rx, ry, rz) end

---@param other CFrame
---@param t number
---@return CFrame
function CFrame:Lerp(other, t) end

-- =============================================================================
-- typeof
-- =============================================================================

---Like type(), but checks __type metatable on tables and ClassName on userdata.
---__type can be a string or a function(self) -> string.
---@param value any
---@return string
function typeof(value) end

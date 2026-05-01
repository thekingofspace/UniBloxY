---@meta

-- =============================================================================
-- Vector2
-- =============================================================================

---@class Vector2
---@field X number
---@field Y number

---@class Vector2lib
---@field zero Vector2
---@field one Vector2
Vector2 = {}

---@param x number
---@param y number
---@return Vector2
function Vector2.new(x, y) end

-- =============================================================================
-- Vector3
-- =============================================================================

---@class Vector3
---@field X number
---@field Y number
---@field Z number

---@class Vector3lib
---@field zero Vector3
---@field one Vector3
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
---@field R number
---@field G number
---@field B number

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

-- =============================================================================
-- UDim
-- =============================================================================

---@class UDim
---@field Scale number
---@field Offset number

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
---@field X UDim
---@field Y UDim

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
---@field Position Vector3
---@field Rotation Vector3

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

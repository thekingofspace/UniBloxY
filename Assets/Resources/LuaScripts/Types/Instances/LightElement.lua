---@meta

---Base for all light instances. Their world placement is taken from the
---parent (anything with a CFrame/Position).
---@class LightElement : Instance
---@field Color Color3
---@field Intensity number
---@field Range number
---@field Brightness number
---@field ShadowType "Soft"|"Realistic"
---@field Active boolean             -- toggle the light off/on
---@field RealTime boolean           -- true: realtime; false: baked
---@field NearPlane number           -- only meaningful when RealTime = true
---@field Strength number            -- only meaningful when RealTime = true
local LightElement = {}

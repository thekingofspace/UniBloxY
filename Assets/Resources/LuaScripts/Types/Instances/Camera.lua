---@meta

---@class Camera : Instance
---@field ClassName "Camera"
---@field CFrame CFrame
---@field FOV number
local Camera = {}

---@return Vector2
function Camera:GetScreenSize() end

---@return Vector2
function Camera:GetWindowSize() end

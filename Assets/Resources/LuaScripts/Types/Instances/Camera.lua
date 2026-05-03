---@meta

---@class Camera : Instance
---@field ClassName "Camera"
---@field CFrame CFrame
---@field FOV number
---@field Aspect number Read-only: pixel width / pixel height of the camera viewport.
local Camera = {}

---@return Vector2
function Camera:GetScreenSize() end

---@return Vector2
function Camera:GetWindowSize() end

---Returns the visible world-space (width, height) at the given distance in front of the camera.
---@param distance number
---@return Vector2
function Camera:GetViewSize(distance) end

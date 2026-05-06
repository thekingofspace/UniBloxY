---@meta

---@class BasePart : Shadable
---@field ClassName "BasePart"
---@field Size Vector3
---@field CFrame CFrame
---@field Shape "Cube"|"Sphere"|"Cylinder"|"Capsule"|"Plane"|"Quad"
---@field Color Color3
---@field Transparency number
---@field Clone fun(self:BasePart) : BasePart
local BasePart = {}

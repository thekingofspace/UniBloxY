---@meta

---@class Frame : ShadableUI
---@field ClassName "Frame"
---@field Size UDim2
---@field Position UDim2
---@field BackgroundColor Color3
---@field BackgroundColor3 Color3 Alias of BackgroundColor.
---@field BackgroundTransparency number 0 = opaque, 1 = invisible.
---@field ClipDescendants boolean If true, descendants are clipped to this frame's rect.
---@field Clone fun(self:Frame):Frame
local Frame = {}

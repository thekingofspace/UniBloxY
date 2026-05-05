---@meta

---@alias ImageScaleType "Stretch"|"Fit"|"Tile"|"Crop"|"Cover"|"Contain"|"Repeat"

---@class ImageLabel : ShadableUI
---@field ClassName "ImageLabel"
---@field Size UDim2
---@field Position UDim2
---@field Image Image
---@field ImageColor Color3
---@field ImageColor3 Color3 Alias of ImageColor.
---@field ImageTransparency number 0 = opaque, 1 = invisible.
---@field ScaleType ImageScaleType How the image fills the rect. Stretch = fill ignoring aspect; Fit = letterbox preserving aspect; Tile = repeat at native size; Crop = fill rect preserving aspect (the texture is cropped to match).
---@field Clone fun(self:ImageLabel):ImageLabel
local ImageLabel = {}

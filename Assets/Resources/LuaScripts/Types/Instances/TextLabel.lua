---@meta

---@class TextLabel : ShadableUI
---@field ClassName "TextLabel"
---@field Size UDim2
---@field Position UDim2
---@field Text string
---@field Font Font
---@field TextSize integer
---@field TextColor Color3
---@field TextColor3 Color3 Alias of TextColor.
---@field TextTransparency number 0 = opaque, 1 = invisible.
---@field TextScaled boolean If true, text auto-fits the rect.
---@field TextXAlignment "Left"|"Center"|"Right"
---@field TextYAlignment "Top"|"Center"|"Bottom"
---@field TextAlignment Placement Combined 9-preset alignment. Setting it updates TextXAlignment + TextYAlignment in one go.
---@field Clone fun(self:TextLabel):TextLabel
local TextLabel = {}

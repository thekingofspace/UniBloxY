---@meta

---@alias Placement "TopLeft"|"Top"|"TopRight"|"Left"|"Center"|"Middle"|"Right"|"BottomLeft"|"Bottom"|"BottomRight"|"Custom"

---@class GUIBase : Instance
---@field Visible boolean If false, this element and all GUI descendants stop rendering.
---@field ZIndex integer Render order among siblings; higher draws on top.
---@field AnchorPoint Vector2 Pivot inside this rect that Position is measured from. (0,0) = top-left (default), (0.5,0.5) = center, (1,1) = bottom-right.
---@field Placement Placement Convenience preset that sets AnchorPoint and Position together. Reading it returns the last preset assigned (or "Custom" when AnchorPoint/Position were set directly).
local GUIBase = {}

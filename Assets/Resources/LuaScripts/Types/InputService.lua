---@meta

---@class InputObject
---@field KeyCode string
---@field KeyCodeId integer

---@alias InputState "Begin"|"End"
---@alias MouseButtonName "Left"|"Right"|"Middle"

---@class Mouse
---@field Position Vector2
---@field Clicked Signal
---@field Moved Signal
---@field ButtonDown Signal
---@field ButtonUp Signal
---@field IsButtonDown fun(self:Mouse, name:MouseButtonName):boolean

---@class InputService
---@field Input Signal
---@field IsKeyDown fun(key:string|integer):boolean
---@field GetMouse fun():Mouse
InputService = {}

return InputService

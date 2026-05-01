---@meta

---@class LuaInputObject
---@field KeyCode string
---@field KeyCodeId integer

---@alias InputState "Begin"|"End"
---@alias MouseButtonName "Left"|"Right"|"Middle"


---@class Mouse
---@field Clicked Signal<string>
---@field Moved Signal<Vector2, LuaVector2>
---@field ButtonDown Signal<MouseButtonName>
---@field ButtonUp Signal<MouseButtonName>
---@field IsButtonDown fun(self:Mouse, name:MouseButtonName):boolean
---@field Position Vector2

---@class InputService
---@field Input Signal<LuaInputObject, InputState>
---@field IsKeyDown fun(key:string|integer):boolean
---@field GetMouse fun():Mouse
InputService = {}

return InputService

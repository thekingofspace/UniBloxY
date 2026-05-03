---@meta

---@class LuaInputObject
---@field ClassName "InputObject"
---@field Device string
---@field KeyCode string
---@field KeyCodeId integer

---@alias InputState "Begin"|"End"
---@alias MouseButtonName "Left"|"Right"|"Middle"


---@class Mouse
---@field Clicked Signal<fun(input:string)>
---@field Moved Signal<fun(Position:Vector2, Delta:Vector2)>
---@field ButtonDown Signal<fun(MouseButton:MouseButtonName)>
---@field ButtonUp Signal<fun(MouseButton:MouseButtonName)>
---@field Scrolled Signal<fun(X:number, Y:number)>
---@field IsButtonDown fun(self:Mouse, name:MouseButtonName):boolean
---@field SetLocked fun(self:Mouse, locked:boolean)
---@field SetVisible fun(self:Mouse, visible:boolean)
---@field Position Vector2


---@class InputService
---@field Input Signal<fun(Input:LuaInputObject, State:InputState)>
---@field IsKeyDown fun(key:string|integer):boolean
---@field GetMouse fun():Mouse
InputService = {}

return InputService

---@meta

---@class LuaInputObject
---@field ClassName "InputObject"
---@field Device string
---@field KeyCode string
---@field KeyCodeId integer

---@alias InputState "Begin"|"End"
---@alias MouseButtonName "Left"|"Right"|"Middle"
---@alias CursorType "Default"|"Arrow"|"Crosshair"|"Cross"|"IBeam"|"Text"|"Caret"|"Wait"|"Busy"|"Hourglass"|"Hand"|"Pointer"


---@class Mouse
---@field Clicked Signal<fun(input:string)>
---@field Moved Signal<fun(Position:Vector2, Delta:Vector2)>
---@field ButtonDown Signal<fun(MouseButton:MouseButtonName)>
---@field ButtonUp Signal<fun(MouseButton:MouseButtonName)>
---@field Scrolled Signal<fun(X:number, Y:number)>
---@field IsButtonDown fun(self:Mouse, name:MouseButtonName):boolean
---@field SetLocked fun(self:Mouse, locked:boolean)
---@field SetVisible fun(self:Mouse, visible:boolean)
---@field SetCursor fun(self:Mouse, cursor:CursorType|Image|Texture|nil, hotspot:Vector2?) Switch the cursor to a built-in type or a custom image.
---@field SetCursorType fun(self:Mouse, name:CursorType, hotspot:Vector2?) Alias of SetCursor for string-only usage.
---@field ResetCursor fun(self:Mouse) Reset back to the OS default arrow.
---@field Position Vector2
---@field Cursor CursorType|string Read-only string identifier of the active cursor. Assign a CursorType, Image, or Texture to change it.
---@field CursorType CursorType|string Alias of Cursor.


---@class InputService
---@field Input Signal<fun(Input:LuaInputObject, State:InputState)>
---@field IsKeyDown fun(key:string|integer):boolean
---@field GetMouse fun():Mouse
InputService = {}

return InputService

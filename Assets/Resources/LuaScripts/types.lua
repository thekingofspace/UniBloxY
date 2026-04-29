---@class Unity
---@field GetClass fun(className:string):table
---@field BindToHeartbeat fun(importance:number, name:string, callback:fun(dt:number)):fun()
---@field Unbind fun(name:string)
---@field GetCamera fun():UnityCamera
Unity = {}

---@class CFrame
---@field X number
---@field Y number
---@field Z number
---@field RX number rotation around X (radians, read-only)
---@field RY number rotation around Y (radians, read-only)
---@field RZ number rotation around Z (radians, read-only)
---@operator mul(CFrame):CFrame

---@class CFrameLib
---@field New fun(x:number, y:number, z:number):CFrame
---@field Angles fun(rx:number, ry:number, rz:number):CFrame
CFrame = {}

---@class UnityCamera
---@field CFrame CFrame
---@field FOV number

---@class task
---@field spawn fun(fn:thread|function, ...:any):thread
---@field defer fun(fn:thread|function, ...:any):thread
---@field delay fun(seconds:number, fn:thread|function, ...:any):thread
---@field wait fun(seconds:number?):number

---@class InputObject
---@field KeyName string
---@field KeyCode integer

---@class Vector2
---@field X number
---@field Y number

---@class UnityMouse
---@field X number screen-pixel X
---@field Y number screen-pixel Y
---@field AbsolutePosition Vector2
---@field Delta Vector2 per-frame mouse delta (works while cursor is locked)
---@field Locked boolean
---@field OnMove fun(callback:fun(delta:Vector2)):fun()

---@class UserInput
---@field OnInput fun(callback:fun(input:InputObject, state:string)):fun()
---@field GetMouse fun():UnityMouse
---@field SetMouseLocked fun(locked:boolean)
UserInput = {}

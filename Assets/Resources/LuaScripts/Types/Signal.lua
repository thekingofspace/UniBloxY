---@meta

---@class SignalConnection
---@field Connected boolean
---@field Disconnect fun(self:SignalConnection)

---@class Signal
---@field Connect fun(self:Signal, callback:fun(...:any)):SignalConnection
---@field Once fun(self:Signal, callback:fun(...:any)):SignalConnection
---@field Wait fun(self:Signal):...
local Signal = {}

return Signal

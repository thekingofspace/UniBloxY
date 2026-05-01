---@meta

---@class SignalConnection
---@field Connected boolean
---@field Disconnect fun(self:SignalConnection)

---@class Signal<T...>
---@field Connect fun(self: Signal<T...>, callback: fun(...: T...)): SignalConnection
---@field Once fun(self: Signal<T...>, callback: fun(...: T...)): SignalConnection
---@field Wait fun(self: Signal<T...>): T...
local Signal = {}

return Signal
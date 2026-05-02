---@meta

---@class RunService
---@field Heartbeat Signal<fun(dt:number)>
---@field GetEnvironment fun():"InStudio"|"Deployed"
---@field Close fun()
---@field BindToClose fun(callback:fun()):fun()
RunService = {}

return RunService

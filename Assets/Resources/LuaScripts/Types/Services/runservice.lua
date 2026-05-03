---@meta

---@class RunService
---@field Heartbeat Signal<fun(dt:number)>
---@field FPS number
---@field GetFPS fun():number
---@field GetEnvironment fun():"InStudio"|"Deployed"
---@field Close fun()
---@field BindToClose fun(callback:fun()):fun()
RunService = {}

return RunService

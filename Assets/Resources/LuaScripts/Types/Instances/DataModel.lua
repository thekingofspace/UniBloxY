---@meta

---@class DataModel : Instance
---@field ClassName "DataModel"
---@field CurrentCamera Camera?
local DataModel = {}

---@generic K: keyof CreatableInstances
---@param name string
---@param className K
---@return CreatableInstances[K]?
function DataModel:ObjectAsInstance(name, className) end

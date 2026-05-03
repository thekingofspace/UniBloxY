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

---Walks the Unity scene starting at `entryPoint` (a GameObject name, or "" for the
---entire active scene) and runs `transform` on every GameObject in the subtree.
---The callback should return a ClassName string to convert that GameObject into a
---LuaInstance of that class, or nil to skip it (descendants are still visited).
---Skipped GameObjects do not become parents — their converted descendants attach
---to the nearest converted ancestor (or `game` if none).
---@param entryPoint string
---@param transform fun(name:string):string?
---@return Instance[]
function DataModel:ConvertToInstance(entryPoint, transform) end

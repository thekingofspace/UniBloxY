---@meta

---@class DataModel : Instance
---@field ClassName "DataModel"
---@field CurrentCamera Camera?
local DataModel = {}

---Wraps the named scene GameObject as a LuaInstance of `className`. Returns
---the created instance (typed to `className`, like Instance.new), or nil if no
---GameObject with that name was found.
---Throws if the target GameObject has any children — ObjectAsInstance only
---wraps a single object. Use ConvertToInstance to walk a subtree.
---@generic K: keyof CreatableInstances
---@param name string
---@param className K
---@return CreatableInstances[K]?
function DataModel:ObjectAsInstance(name, className) end

---Walks the Unity scene starting at `entryPoint` (a GameObject name, or "" for the
---entire active scene) and runs `transform` on every GameObject in the subtree,
---*including the entry root itself*. The callback should return a ClassName
---string to convert that GameObject into a LuaInstance of that class, or nil to
---skip it (descendants are still visited). Skipped GameObjects do not become
---parents — their converted descendants attach to the nearest converted
---ancestor (or `game` if none).
---Returns an array of every top-level instance created (those parented directly
---to `game`). Deeper instances are reachable through their parents' children.
---@param entryPoint string
---@param transform fun(name:string):string?
---@return Instance[]
function DataModel:ConvertToInstance(entryPoint, transform) end

---Loads the named Unity Scene additively (must be in Build Settings) and runs
---`transform` on every GameObject in the loaded scene, just like
---ConvertToInstance. Returns an array of every top-level instance created.
---@param sceneName string
---@param transform fun(name:string):string?
---@return Instance[]
function DataModel:ImportScene(sceneName, transform) end

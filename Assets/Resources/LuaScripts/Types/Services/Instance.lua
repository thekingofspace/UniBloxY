---@meta

------------------------
-- Base Instance Type --
------------------------

---@class Instance
---@field Name string
---@field ClassName string
---@field Parent Instance?
---@field Moveable boolean  -- read-only; true means descendants follow this instance when it moves
---@field Changed Signal<fun(property: string)>
---@field ChildAdded Signal<fun(child: Instance)>
---@field ChildRemoved Signal<fun(child: Instance)>
---@field AncestryChanged Signal<fun(child: Instance, parent: Instance?)>
---@field AttributeChanged Signal<fun(attribute: string)>
---@field Destroying Signal<fun(instance: Instance)>
local InstanceBase = {}

---@param property string
---@return Signal<fun()>
function InstanceBase:GetPropertyChangedSignal(property) end

---@param attribute string
---@return Signal<fun()>
function InstanceBase:GetAttributeChangedSignal(attribute) end

---@param name string
---@return any
function InstanceBase:GetAttribute(name) end

---@param name string
---@param value any
function InstanceBase:SetAttribute(name, value) end

---@return table<string, any>
function InstanceBase:GetAttributes() end

---@param name string
---@param recursive boolean?
---@return Instance?
function InstanceBase:FindFirstChild(name, recursive) end

---@param name string
---@return Instance?
function InstanceBase:FindFirstAncestor(name) end

---@return Instance[]
function InstanceBase:GetChildren() end

---@return Instance[]
function InstanceBase:GetDescendants() end

---@param other Instance
---@return boolean
function InstanceBase:IsDescendantOf(other) end

---@param other Instance
---@return boolean
function InstanceBase:IsAncestorOf(other) end

function InstanceBase:ClearAllChildren() end
function InstanceBase:Destroy() end

---Clone returns a new instance of the same concrete class. Throws if the
---class is not clonable (most instances are not — only BaseCube, Folder,
---and RenderGroup are clonable in the current setup). Subclasses re-declare
---this method with their own return type so callers get the right class.
---@generic T : Instance
---@param self T
---@return T
function InstanceBase:Clone() end

--------------------------------
-- Concrete Instance Types -----
--------------------------------

--------------------------------
-- Creatable Mapping ----------
--------------------------------
--------------------------------
-- Constructor -----------------
--------------------------------

---@class InstanceConstructor
local InstanceConstructor = {}

---@generic K: keyof CreatableInstances
---@param className K
---@param parent Instance?
---@return CreatableInstances[K]
function InstanceConstructor.New(className, parent) end

------------------
-- Type Aliases --
------------------

---@alias Instance InstanceBase

------------------
-- Globals ------
------------------

---@type InstanceConstructor
Instance = InstanceConstructor

---@type DataModel
game = nil

return Instance
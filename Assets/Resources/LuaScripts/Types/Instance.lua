---@meta

------------------------
-- Base Instance Type --
------------------------

---@class Instance
---@field Name string
---@field ClassName string
---@field Parent Instance?
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

---@return Instance
function InstanceBase:Clone() end

--------------------------------
-- Concrete Instance Types -----
--------------------------------

---@class Folder : Instance
local Folder = {}

---@class DataModel : Instance
local DataModel = {}

--------------------------------
-- Creatable Mapping ----------
--------------------------------

---@class CreatableInstances
---@field Folder Folder
---@field DataModel DataModel

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
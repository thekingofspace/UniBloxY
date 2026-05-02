---@meta

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

---@param property string
---@return Signal<fun()>
function Instance:GetPropertyChangedSignal(property) end

---@param attribute string
---@return Signal<fun()>
function Instance:GetAttributeChangedSignal(attribute) end

---@param name string
---@return any
function Instance:GetAttribute(name) end

---@param name string
---@param value any
function Instance:SetAttribute(name, value) end

---@return table<string, any>
function Instance:GetAttributes() end

---@param name string
---@param recursive boolean?
---@return Instance?
function Instance:FindFirstChild(name, recursive) end

---@param name string
---@return Instance?
function Instance:FindFirstAncestor(name) end

---@return Instance[]
function Instance:GetChildren() end

---@return Instance[]
function Instance:GetDescendants() end

---@param other Instance
---@return boolean
function Instance:IsDescendantOf(other) end

---@param other Instance
---@return boolean
function Instance:IsAncestorOf(other) end

function Instance:ClearAllChildren() end
function Instance:Destroy() end

---@return Instance
function Instance:Clone() end


---@class CreatableInstances
---@field Folder Folder
---@field DataModel DataModel

---@class InstanceConstructor
local InstanceConstructor = {}

---@generic K: keyof CreatableInstances
---@param className K
---@param parent Instance?
---@return CreatableInstances[K]
function InstanceConstructor.New(className, parent) end


---@type InstanceConstructor
Instance = InstanceConstructor

---@type DataModel
game = nil

return Instance```
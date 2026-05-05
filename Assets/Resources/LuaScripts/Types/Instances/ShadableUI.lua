---@meta

---@class ShadableUI : GUIBase
---@field AddShader fun(self:ShadableUI, shader:Shader)
---@field RemoveShader fun(self:ShadableUI, shader:Shader):boolean
---@field ListShaders fun(self:ShadableUI):Shader[]
---@field SetShaderData fun(self:ShadableUI, shader:Shader, name:string, value:any)
---@field AddMaterial fun(self:ShadableUI, material:Material)
---@field RemoveMaterial fun(self:ShadableUI, material:Material):boolean
---@field ListMaterials fun(self:ShadableUI):Material[]
---@field SetMaterialProperty fun(self:ShadableUI, material:Material, name:string, value:any)
---@field SetMaterialData fun(self:ShadableUI, material:Material, name:string, value:any)
local ShadableUI = {}

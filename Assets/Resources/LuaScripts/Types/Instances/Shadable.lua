---@meta

---@class Shader
---@field Name string

---@class Texture
---@field Name string
---@field Width integer
---@field Height integer
---@field WrapMode string
---@field FilterMode string

---@class Material
---@field Name string
---@field Shader Shader
---@field Color Color3
---@field Texture Texture
---@field Tiling Vector2
---@field Offset Vector2
---@field Repeat number
---@field Smoothness number
---@field Metallic number
---@field RenderQueue integer
---@field Set fun(self:Material, prop:string, value:any)
---@field Get fun(self:Material, prop:string):any
---@field EnableKeyword fun(self:Material, keyword:string)
---@field DisableKeyword fun(self:Material, keyword:string)

---@class Shadable : Renderable
---@field AddShader fun(self:Shadable, shader:Shader)
---@field RemoveShader fun(self:Shadable, shader:Shader):boolean
---@field ListShaders fun(self:Shadable):Shader[]
---@field SetShaderData fun(self:Shadable, shader:Shader, name:string, value:any)
---@field AddMaterial fun(self:Shadable, material:Material)
---@field RemoveMaterial fun(self:Shadable, material:Material):boolean
---@field ListMaterials fun(self:Shadable):Material[]
---@field SetMaterialProperty fun(self:Shadable, material:Material, name:string, value:any)
---@field SetMaterialData fun(self:Shadable, material:Material, name:string, value:any)
local Shadable = {}
+
---@meta

---@class Shader
---@field Name string

---@class Shadable : Renderable
---@field AddShader fun(self:Shadable, shader:Shader)
---@field RemoveShader fun(self:Shadable, shader:Shader):boolean
---@field ListShaders fun(self:Shadable):Shader[]
---@field SetShaderData fun(self:Shadable, shader:Shader, name:string, value:any)
local Shadable = {}

---@meta

---@class AssetService
---@field GetShader fun(name:string):Shader
---@field Get fun(name:string):Shader
---@field GetMaterial fun(name:string):Material
---@field GetTexture fun(name:string):Texture
---@field CreateMaterial fun(shaderName:string, name:string?):Material
---@field ShaderLoaded Signal<fun(name:string)> Fires with the asset name when a shader finishes loading.
---@field AssetLoaded Signal<fun(name:string)> Fires with the asset name when a material/texture finishes loading.
AssetService = {}

return AssetService

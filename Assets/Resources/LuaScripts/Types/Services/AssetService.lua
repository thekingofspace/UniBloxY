---@meta

---@class Image
---@field ClassName "Image"
---@field Name string
---@field Width integer
---@field Height integer

---@class Font
---@field ClassName "Font"
---@field Name string

---@class AssetService
---@field GetShader fun(name:string):Shader
---@field Get fun(name:string):Shader
---@field GetMaterial fun(name:string):Material
---@field GetTexture fun(name:string):Texture
---@field GetImage fun(name:string):Image Loads a sprite/texture from Resources/Images/<name>.
---@field GetFont fun(name:string):Font Loads a font from Resources/Fonts/<name> (built-in fonts work too).
---@field CreateMaterial fun(shaderName:string, name:string?):Material
---@field ShaderLoaded Signal<fun(name:string)> Fires with the asset name when a shader finishes loading.
---@field AssetLoaded Signal<fun(name:string)> Fires with the asset name when a material/texture/image/font finishes loading.
AssetService = {}

return AssetService

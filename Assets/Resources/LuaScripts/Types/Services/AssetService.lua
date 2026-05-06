---@meta

---@class Image
---@field ClassName "Image"
---@field Name string
---@field Width integer
---@field Height integer

---@class Font
---@field ClassName "Font"
---@field Name string

---@class Mesh
---@field ClassName "Mesh"
---@field Name string
---@field VertexCount integer
---@field SubMeshCount integer

---@class AssetService
---@field GetShader fun(name:string):Shader
---@field Get fun(name:string):Shader
---@field GetMaterial fun(name:string):Material
---@field GetTexture fun(name:string):Texture
---@field GetImage fun(name:string):Image Loads a sprite/texture from Resources/Images/<name>.
---@field GetFont fun(name:string):Font Loads a font from Resources/Fonts/<name> (built-in fonts work too).
---@field GetMesh fun(name:string):Mesh Loads a mesh from Resources/Meshes/<name> (Mesh asset, MeshFilter prefab, or SkinnedMeshRenderer prefab).
---@field CreateMaterial fun(shaderName:string, name:string?):Material
---@field ShaderLoaded Signal<fun(name:string)> Fires with the asset name when a shader finishes loading.
---@field AssetLoaded Signal<fun(name:string)> Fires with the asset name when a material/texture/image/font/mesh finishes loading.
AssetService = {}

return AssetService

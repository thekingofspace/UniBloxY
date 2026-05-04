---@meta

---@class ShaderService
---@field GetShader fun(name:string):Shader
---@field Get fun(name:string):Shader
---@field GetMaterial fun(name:string):Material
---@field GetTexture fun(name:string):Texture
---@field CreateMaterial fun(shaderName:string, name:string?):Material
---@field ShaderLoaded Signal<fun(loaded:number, requested:number)> Fires (loaded:number, requested:number) when a shader finishes loading.
---@field ShadersFinishedLoading Signal<fun()> Fires when all pending shader loads complete.
---@field AssetLoaded Signal<fun(loaded:number, requested:number)> Fires (loaded:number, requested:number) when a non-shader asset (material/texture) finishes loading.
---@field AssetsFinishedLoading Signal<fun()> Fires when all pending non-shader asset loads complete.
---@field ShadersLoaded boolean True when no shader load is pending.
---@field AssetsLoaded boolean True when no non-shader asset load is pending.
ShaderService = {}

return ShaderService

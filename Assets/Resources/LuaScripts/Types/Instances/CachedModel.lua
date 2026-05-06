---@meta

---CachedModel renders a mesh that shares its material across all instances
---using the same material reference. Per-instance Color, Transparency and
---Texture are pushed via a MaterialPropertyBlock so batching/instancing is
---preserved. It cannot be shaded (no AddShader/AddMaterial) and has no
---Animator hooks.
---@class CachedModel : Renderable
---@field ClassName "CachedModel"
---@field Size Vector3
---@field CFrame CFrame
---@field Color Color3 Per-instance tint applied via a MaterialPropertyBlock.
---@field Transparency number Per-instance alpha applied via a MaterialPropertyBlock.
---@field Model Mesh|string|nil The mesh to render. Assigning a string loads from Resources/Meshes/<name>.
---@field Material Material|string|nil The shared material — used directly without instancing.
---@field Texture Texture|string|nil Per-instance texture override applied via the property block.
---@field Clone fun(self:CachedModel):CachedModel
local CachedModel = {}

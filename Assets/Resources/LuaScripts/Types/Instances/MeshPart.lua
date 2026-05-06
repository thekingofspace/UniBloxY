---@meta

---@class MeshPart : BasePart
---@field ClassName "MeshPart"
---@field Model Mesh|string|nil The mesh to render. Assigning a string loads from Resources/Meshes/<name> via AssetService.
---@field Skeleton Skeleton|string|nil When set, the GameObject is built from the skeleton prefab so animation can drive bones. Assigning a string imports via AnimatorService.
---@field Animator Animator|nil Read-only handle returned by LinkAnimator (nil until linked).
---@field LinkAnimator fun(self:MeshPart):Animator Adds an Animation runtime to this MeshPart's GameObject and returns the cached Animator handle.
---@field Clone fun(self:MeshPart):MeshPart
local MeshPart = {}

---@meta

-- =============================================================================
-- RaycastParams
-- =============================================================================

---@class RaycastParams
---@field ClassName "RaycastParams"
---@field Start Vector3
---@field Direction Vector3
---@field MaxDistance number   -- defaults to 1000
---@field IgnoreUnityObjects boolean
---@field Transformer fun(hit: Instance|UnityHit):boolean   -- return true to accept the hit
local RaycastParams = {}

---@class RaycastParamslib
RaycastParams = {}

---@return RaycastParams
function RaycastParams.new() end

-- =============================================================================
-- RaycastResult / UnityHit
-- =============================================================================

---@class UnityHit
---@field ClassName "UnityObject"
---@field CFrame CFrame
---@field Name string

---@class RaycastResult
---@field ClassName "RaycastResult"
---@field EndPosition Vector3
---@field Normal Vector3
---@field Distance number
---@field Object Instance?         -- nil when the hit is a raw Unity object
---@field UnityObject string?      -- name of the raw Unity object, when applicable

-- =============================================================================
-- PostProcessing (returned by Lighting:GetPostProcessing())
-- =============================================================================

---@class PostProcessing
---@field ClassName "PostProcessing"
local PostProcessing = {}

---@param key string
---@return any
function PostProcessing:Get(key) end

---@param key string
---@param value any
function PostProcessing:Set(key, value) end

---@param key string
---@return boolean
function PostProcessing:Has(key) end

-- =============================================================================
-- Lighting (global)
-- =============================================================================

---@class Lighting
---@field Ambient Color3
---@field FogColor Color3
---@field FogStart number
---@field FogEnd number
---@field FogEnabled boolean
---@field Exposure number
---@field DirtTexture Texture?
---@field Skybox Texture?
---@field BloomThreshold number
---@field BloomIntensity number
---@field VignetteIntensity number
---@field Saturation number
---@field Contrast number
Lighting = {}

---Casts a ray from `start` toward `target`. With `params`, the per-hit
---`Transformer` is invoked for each candidate hit; return true to stop
---and return the hit, false to keep searching.
---@param start Vector3
---@param target Vector3
---@param params RaycastParams?
---@return RaycastResult?
function Lighting:Raycast(start, target, params) end

---Returns the post-processing override bag (read/write any key found in
---the volume's Override > Post Processing section).
---@return PostProcessing
function Lighting:GetPostProcessing() end

return Lighting

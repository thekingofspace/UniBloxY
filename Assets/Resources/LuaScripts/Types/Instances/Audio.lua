---@meta

---An audio clip imported from Resources/Sounds/<name> via AssetService:GetSound.
---Pass directly to Sound.SoundId, or pass the name string to have Sound import it.
---@class Audio
---@field ClassName "Audio"
---@field Name string
---@field Length number Clip length in seconds.
---@field Channels integer
---@field Frequency integer
---@field Samples integer
local Audio = {}

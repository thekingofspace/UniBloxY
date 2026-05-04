---@meta

---@class Camera : Instance
---@field ClassName "Camera"
---@field CFrame CFrame
---@field FOV number
---@field Aspect number Read-only: pixel width / pixel height of the camera viewport.
---@field FullScreenMode string Read-only.
---@field IsFullScreen boolean Read-only.
---@field WindowResized Signal Fires when the window/screen size changes. Args: (Vector2 newSize).
local Camera = {}

---@return Vector2
function Camera:GetScreenSize() end

---@return Vector2
function Camera:GetWindowSize() end

---Returns the visible world-space (width, height) at the given distance in front of the camera.
---@param distance number
---@return Vector2
function Camera:GetViewSize(distance) end

---Switches the application window mode.
---  mode = "borderless"  → fullscreen window with no border (default)
---  mode = "fullscreen"  → exclusive fullscreen
---  mode = "windowed"    → bordered window (pass width, height to size it)
---  mode = "maximized"   → maximized window (Windows only)
---@param mode string
---@param width? integer
---@param height? integer
function Camera:SetFullScreen(mode, width, height) end

---@meta

---@class Listener
---@field ClassName "Listener"
---@field Mode "Mouse"|"Instance"
---@field Destroyed boolean
---@field OnEnter Signal<fun(tracker:Instance)> Fires when a tracker becomes hovered (mouse) or starts overlapping the target (instance).
---@field OnLeave Signal<fun(tracker:Instance)> Fires when a tracker stops being hovered/overlapping. Always paired with a prior OnEnter.
---@field OnActivated Signal<fun(tracker:Instance)> Mouse mode only — fires when the left button is pressed while hovering a tracker.
---@field OnRelease Signal<fun(tracker:Instance)> Mouse mode only — fires when the left button is released, or when the cursor leaves while pressed.
---@field AddTracker fun(self:Listener, target:Instance) Begin watching the given instance.
---@field RemoveTracker fun(self:Listener, target:Instance):boolean Stop watching. Fires any pending OnRelease/OnLeave first.
---@field GetTrackers fun(self:Listener):Instance[]
---@field Destroy fun(self:Listener) Drain pending exit signals and stop ticking.
local Listener = {}


---@class ListenerService
---@field ListenToMouse fun():Listener Build a Listener that fires on cursor hover/press of its trackers (UI rect or 3D raycast). Callable as `ListenerService.ListenToMouse()` or `ListenerService:ListenToMouse()`.
---@field ListenToInstance fun(target:Instance):Listener Build a Listener that fires when its trackers overlap `target`. 2D-vs-2D or 3D-vs-3D only. Callable as `ListenerService.ListenToInstance(target)` or `ListenerService:ListenToInstance(target)`.
ListenerService = {}

return ListenerService

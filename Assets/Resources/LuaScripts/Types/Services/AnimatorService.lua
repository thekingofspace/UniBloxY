---@meta

---@class Skeleton
---@field ClassName "Skeleton"
---@field Name string

---@class Animation
---@field ClassName "Animation"
---@field Name string
---@field Length number Length of the underlying clip in seconds.
---@field Looped boolean True if the imported clip is marked as looping.

---@class AnimationTrack
---@field ClassName "AnimationTrack"
---@field Name string
---@field IsPlaying boolean
---@field Length number
---@field Looped boolean
---@field Speed number
---@field TimePosition number
---@field Weight number Per-track blend weight (0..1).
---@field Priority integer Layer used for blending. Higher priorities override lower; tracks at different priorities all play in parallel.
---@field Additive boolean If true, the track is layered as an additive pose (AnimationBlendMode.Additive); otherwise it blends normally.
---@field Play fun(self:AnimationTrack, fadeTime:number?) Plays the track. fadeTime > 0 crossfades from any track on the same Priority.
---@field Blend fun(self:AnimationTrack, weight:number?, fadeTime:number?) Plays additively without stopping other tracks. fadeTime ramps weight to the target over fadeTime seconds.
---@field CrossFade fun(self:AnimationTrack, fadeTime:number) Crossfades to this track, fading out every other track on the animator over fadeTime seconds.
---@field Fade fun(self:AnimationTrack, targetWeight:number, fadeTime:number) Fades the track's Weight to a target value over fadeTime seconds.
---@field Stop fun(self:AnimationTrack, fadeTime:number?) Stops the track. fadeTime > 0 fades the weight to 0 first, then stops.
local AnimationTrack = {}

---@class Animator
---@field ClassName "Animator"
---@field IsPlaying boolean True while any track on this animator is playing.
---@field LoadAnimation fun(self:Animator, animation:Animation):AnimationTrack Adds the clip to the animator and returns a track keyed by the animation's Name. Calling again with the same Animation returns the cached track.
---@field GetTrack fun(self:Animator, name:string):AnimationTrack?
---@field CrossFade fun(self:Animator, track:AnimationTrack, fadeTime:number?):AnimationTrack Convenience: plays the supplied track with a crossfade over fadeTime seconds (default 0.25).
---@field StopAll fun(self:Animator)
local Animator = {}

---@class AnimatorService
---@field ImportSkeleton fun(name:string):Skeleton Loads a rigged GameObject prefab from Resources/Skeletons/<name>. Use it as the MeshPart.Skeleton to drive bones at runtime.
---@field ImportAnimation fun(name:string):Animation Loads an AnimationClip from Resources/Animations/<name>.
---@field SkeletonImported Signal<fun(name:string)>
---@field AnimationImported Signal<fun(name:string)>
AnimatorService = {}

return AnimatorService

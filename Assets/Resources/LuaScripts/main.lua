-- =============================================================================
-- Tiny first-person shooter
--   Mouse           look around (locked & hidden)
--   WASD            move on the XZ plane (relative to where you're looking)
--   Space           lunge in the current move direction — camera slides with you
--   Left mouse      fire a glowing neon bullet down your sightline
--   Escape          pause / unpause (cursor returns)
-- =============================================================================

for _, child in ipairs(game:GetChildren()) do
    pcall(function() child:Destroy() end)
end

local cam = game.CurrentCamera
local mouse = InputService:GetMouse()

cam.FOV = 90
mouse:SetLocked(true)
mouse:SetVisible(false)

-- ----------------------------------------------------------------------------
-- Lighting — directional sun angled down so the world has shape.
-- ----------------------------------------------------------------------------

local sun = Instance.new("GlobalLight", game)
sun.Name = "Sun"
sun.Color = Color3.fromRGB(255, 245, 220)
sun.Brightness = 1.4
sun.Rotation = Vector3.new(50, -30, 0)   -- down + slightly to one side
sun.ShadowType = "Soft"

-- ----------------------------------------------------------------------------
-- World
-- ----------------------------------------------------------------------------

local FLOOR_Y      = 0
local FLOOR_SIZE   = Vector3.new(80, 1, 80)
local GRAVITY      = -36
local PLAYER_SIZE  = Vector3.new(1, 2, 1)
local TARGET_SIZE  = Vector3.new(2, 2, 2)
local BULLET_SIZE  = Vector3.new(0.35, 0.35, 1.4)   -- elongated tracer

local floor = Instance.new("BasePart", game)
floor.Name = "Floor"
floor.Render = true
floor.Size = FLOOR_SIZE
floor.CFrame = CFrame.new(Vector3.new(0, FLOOR_Y - FLOOR_SIZE.Y * 0.5, 0))
floor.Color = Color3.fromRGB(60, 65, 75)

-- Neon material for the bullet head — Custom/Neon shader, hot orange.
local neonMat = AssetService.CreateMaterial("Neon", "BulletNeon")
neonMat.Color = Color3.fromRGB(255, 110, 30)
neonMat:Set("_Glow", 4.0)
neonMat:Set("_RimPower", 1.6)
neonMat:Set("_RimBoost", 5.0)

-- Trail material — Custom/TrailNeon, additive with built-in alpha falloff.
local trailMat = AssetService.CreateMaterial("TrailNeon", "BulletTrail")
trailMat.Color = Color3.fromRGB(255, 130, 40)
trailMat:Set("_Glow", 5.0)
trailMat:Set("_Fade", 1.6)
trailMat:Set("_Rim", 1.5)

-- ----------------------------------------------------------------------------
-- Physics
-- ----------------------------------------------------------------------------

local bodies = {}

local function addBody(part)
    local b = {
        Part        = part,
        Velocity    = Vector3.new(0, 0, 0),
        HalfHeight  = part.Size.Y * 0.5,
        OnGround    = false,
    }
    bodies[#bodies + 1] = b
    return b
end

local function removeBody(body)
    for i = #bodies, 1, -1 do
        if bodies[i] == body then table.remove(bodies, i); return end
    end
end

local REST_EPSILON_SQ = 0.0001

local function stepBody(body, dt)
    if not body.Part or not body.Part.Parent then return end

    -- A grounded body with effectively zero velocity is at rest. Integrating
    -- it still produces a tiny gravity tick that gets clamped right back —
    -- and every cycle of that allocates ~10 LuaVector3/CFrame objects. Bail
    -- out before any of that runs so resting targets cost nothing per frame.
    if body.OnGround then
        local v = body.Velocity
        if v.X * v.X + v.Y * v.Y + v.Z * v.Z < REST_EPSILON_SQ then
            body.JustLanded = false
            return
        end
    end

    body.Velocity = body.Velocity + Vector3.new(0, GRAVITY * dt, 0)
    local pos = body.Part.CFrame.Position + body.Velocity * dt

    local minY = FLOOR_Y + body.HalfHeight
    local landed = false
    if pos.Y <= minY then
        landed = (not body.OnGround) and body.Velocity.Y < -3
        pos = Vector3.new(pos.X, minY, pos.Z)
        body.Velocity = Vector3.new(body.Velocity.X, 0, body.Velocity.Z)
        body.OnGround = true
    else
        body.OnGround = false
    end

    if body.OnGround then
        local damp = math.max(0, 1 - 6 * dt)
        body.Velocity = Vector3.new(body.Velocity.X * damp, body.Velocity.Y, body.Velocity.Z * damp)
    end

    body.Part.CFrame = CFrame.new(pos)
    body.JustLanded = landed
end

-- ----------------------------------------------------------------------------
-- Player
-- ----------------------------------------------------------------------------

local player = Instance.new("BasePart", game)
player.Name = "Player"
player.Render = true
player.Size = PLAYER_SIZE
player.CFrame = CFrame.new(Vector3.new(0, 5, 0))
player.Color = Color3.fromRGB(60, 160, 255)
player.Transparency = 1
player.CastShadow = false

local playerBody = addBody(player)

local PLAYER_SPEED  = 14       -- top speed (high-pace)
local AIR_CONTROL   = 4        -- exponential blend rate while airborne
local LUNGE_IMPULSE = 38
local HEAD_HEIGHT   = 0.7

-- ----------------------------------------------------------------------------
-- Gun — a small cube held to the right of the camera. Bullets spawn from
-- the muzzle (its forward face) so the tracer reads as coming from the gun.
-- ----------------------------------------------------------------------------

local GUN_SIZE   = Vector3.new(0.35, 0.35, 0.9)
local GUN_RIGHT  = 0.95   -- offset right of camera
local GUN_DOWN   = 0.65   -- offset below camera
local GUN_FWD    = 0.6    -- offset forward of camera

local gun = Instance.new("BasePart", game)
gun.Name = "Gun"
gun.Render = true
gun.Size = GUN_SIZE
gun.Color = Color3.fromRGB(40, 40, 45)
gun.CastShadow = false

local gunMuzzlePos = Vector3.new(0, 0, 0)
local gunMuzzleDir = Vector3.new(0, 0, 1)

-- ----------------------------------------------------------------------------
-- Speed FOV — camera widens as the player moves faster, giving a subtle
-- sense of acceleration. Smoothed so the FOV ramps instead of snapping.
-- ----------------------------------------------------------------------------

local FOV_BASE   = 90
local FOV_MAX    = 108
local FOV_MIN_SPEED = PLAYER_SPEED * 0.6
local FOV_MAX_SPEED = PLAYER_SPEED * 1.8
cam.FOV = FOV_BASE
local fovT = 0

-- ----------------------------------------------------------------------------
-- Pause state
-- ----------------------------------------------------------------------------

local paused = false
local pauseUI = nil

local function buildPauseUI()
    local frame = Instance.new("Frame", game)
    frame.Name = "PauseMenu"
    frame.Size = UDim2.fromScale(1, 1)
    frame.Position = UDim2.fromOffset(0, 0)
    frame.BackgroundColor = Color3.new(0, 0, 0)
    frame.BackgroundTransparency = 0.45

    local title = Instance.new("TextLabel", frame)
    title.Name = "Title"
    title.Size = UDim2.new(0.6, 0, 0.18, 0)
    title.AnchorPoint = Vector2.new(0.5, 0.5)
    title.Position = UDim2.new(0.5, 0, 0.42, 0)
    title.Text = "PAUSED"
    title.TextSize = 96
    title.TextScaled = true
    title.TextColor = Color3.fromRGB(255, 130, 40)
    title.TextAlignment = "Center"

    local hint = Instance.new("TextLabel", frame)
    hint.Name = "Hint"
    hint.Size = UDim2.new(0.4, 0, 0.05, 0)
    hint.AnchorPoint = Vector2.new(0.5, 0.5)
    hint.Position = UDim2.new(0.5, 0, 0.56, 0)
    hint.Text = "Press Esc to resume"
    hint.TextSize = 28
    hint.TextScaled = true
    hint.TextColor = Color3.new(1, 1, 1)
    hint.TextAlignment = "Center"

    return frame
end

local function setPaused(v)
    paused = v
    if v then
        if not pauseUI then pauseUI = buildPauseUI() end
        pauseUI.Visible = true
        mouse:SetLocked(false)
        mouse:SetVisible(true)
    else
        if pauseUI then pauseUI.Visible = false end
        mouse:SetLocked(true)
        mouse:SetVisible(false)
    end
end

-- ----------------------------------------------------------------------------
-- Mouse look
-- ----------------------------------------------------------------------------

local yawDeg   = 0
local pitchDeg = 10
local SENS     = 0.15

mouse.Moved:Connect(function(_, delta)
    if paused then return end
    yawDeg = yawDeg + delta.X * SENS
    pitchDeg = math.max(-89, math.min(89, pitchDeg - delta.Y * SENS))
end)

local function yawBasis()
    local yr = math.rad(yawDeg)
    local fwd   = Vector3.new(math.sin(yr), 0,  math.cos(yr))
    local right = Vector3.new(math.cos(yr), 0, -math.sin(yr))
    return fwd, right
end

local function cameraForward()
    local yr = math.rad(yawDeg)
    local pr = math.rad(pitchDeg)
    local cp = math.cos(pr)
    return Vector3.new(math.sin(yr) * cp, -math.sin(pr), math.cos(yr) * cp)
end

local function moveDir()
    local f, s = 0, 0
    if InputService.IsKeyDown("W") then f = f + 1 end
    if InputService.IsKeyDown("S") then f = f - 1 end
    if InputService.IsKeyDown("A") then s = s - 1 end
    if InputService.IsKeyDown("D") then s = s + 1 end
    local m = math.sqrt(f * f + s * s)
    if m == 0 then return Vector3.new(0, 0, 0), false end
    f, s = f / m, s / m
    local fwd, right = yawBasis()
    return fwd * f + right * s, true
end

local lastMoveDir = Vector3.new(0, 0, 1)

-- ----------------------------------------------------------------------------
-- Camera shake & lunge spring (clean critical-damped slide).
-- ----------------------------------------------------------------------------

local shakeMag    = 0
local lungeOffset = Vector3.new(0, 0, 0)
local lungeVel    = Vector3.new(0, 0, 0)

local LUNGE_STIFFNESS = 9
local LUNGE_DAMPING   = 6
local LUNGE_KICK      = 8

local function addShake(amount)
    if amount > shakeMag then shakeMag = amount end
end

-- ----------------------------------------------------------------------------
-- Targets
-- ----------------------------------------------------------------------------

local TARGET_HITS = 3

local function makeTarget(pos)
    local part = Instance.new("BasePart", game)
    part.Name = "Target"
    part.Render = true
    part.Size = TARGET_SIZE
    part.CFrame = CFrame.new(pos)
    part.Color = Color3.fromRGB(220, 70, 70)

    local body = addBody(part)

    -- Health bar floating above the target. BillboardGui is parented to the
    -- part so it follows the target around and dies with it.
    local bb = Instance.new("BillboardGui", part)
    bb.Name = "HealthBar"
    bb.Size = UDim2.fromScale(3, 0.8)
    bb.Offset = Vector3.new(0, TARGET_SIZE.Y * 0.5 + 0.8, 0)
    bb.AlwaysOnTop = false

    local track = Instance.new("Frame", bb)
    track.Name = "Track"
    track.Size = UDim2.fromScale(1, 1)
    track.BackgroundColor = Color3.fromRGB(30, 10, 10)

    local fill = Instance.new("Frame", track)
    fill.Name = "Fill"
    fill.Size = UDim2.fromScale(1, 1)
    fill.Position = UDim2.fromOffset(0, 0)
    fill.BackgroundColor = Color3.fromRGB(50, 200, 70)

    return { Part = part, Body = body, Fill = fill, Hits = 0, Dying = false }
end

local targets = {
    makeTarget(Vector3.new(  8, 1.5,  10)),
    makeTarget(Vector3.new( -9, 1.5,  14)),
    makeTarget(Vector3.new(  2, 1.5,  20)),
    makeTarget(Vector3.new(  11, 1.5,  20)),
    makeTarget(Vector3.new(  18, 1.5,  20)),
}

-- Pick a random spawn position on the floor, away from the player so a
-- respawn doesn't drop the new target on top of them.
local function randomSpawnPos()
    local extent = FLOOR_SIZE.X * 0.5 - 4
    local px = player.CFrame.Position.X
    local pz = player.CFrame.Position.Z
    for _ = 1, 8 do
        local x = (math.random() * 2 - 1) * extent
        local z = (math.random() * 2 - 1) * extent
        local dx, dz = x - px, z - pz
        if dx * dx + dz * dz >= 36 then
            return Vector3.new(x, 1.5 + TARGET_SIZE.Y * 0.5, z)
        end
    end
    return Vector3.new(0, 1.5 + TARGET_SIZE.Y * 0.5, 18)
end

local function overlaps(a, b)
    local ap, bp = a.CFrame.Position, b.CFrame.Position
    local as, bs = a.Size, b.Size
    return  math.abs(ap.X - bp.X) <= (as.X + bs.X) * 0.5
        and math.abs(ap.Y - bp.Y) <= (as.Y + bs.Y) * 0.5
        and math.abs(ap.Z - bp.Z) <= (as.Z + bs.Z) * 0.5
end

-- Replace `target` with a freshly spawned one at a random floor position.
-- The original entry in `targets` is overwritten so the array length stays
-- constant — no need to remove and append.
local function respawnTarget(target)
    for i, t in ipairs(targets) do
        if t == target then
            targets[i] = makeTarget(randomSpawnPos())
            return
        end
    end
end

local function fadeAndDestroy(target, duration)
    if target.Dying then return end
    target.Dying = true
    if target.Body then removeBody(target.Body); target.Body = nil end

    local elapsed = 0
    local conn
    conn = RunService.Heartbeat:Connect(function(dt)
        if paused then return end
        elapsed = elapsed + dt
        local t = math.min(1, elapsed / duration)
        if target.Part and target.Part.Parent then
            target.Part.Transparency = t
        end
        if t >= 1 then
            conn:Disconnect()
            if target.Part and target.Part.Parent then target.Part:Destroy() end
            respawnTarget(target)
        end
    end)
end

local function registerHit(target, bulletDir)
    if target.Dying then return end
    target.Hits = target.Hits + 1

    -- Shrink the green fill as health drops, lerping its color toward red.
    local frac = math.max(0, 1 - target.Hits / TARGET_HITS)
    if target.Fill and target.Fill.Parent then
        target.Fill.Size = UDim2.new(frac, 0, 1, 0)
        local r = math.floor(50 + (1 - frac) * 180)
        local g = math.floor(50 + frac * 170)
        target.Fill.BackgroundColor = Color3.fromRGB(r, g, 60)
    end

    local push = Vector3.new(bulletDir.X, 0, bulletDir.Z)
    local mag = math.sqrt(push.X * push.X + push.Z * push.Z)
    if mag > 0 then
        push = Vector3.new(push.X / mag, 0, push.Z / mag) * 14
        target.Body.Velocity = target.Body.Velocity + push + Vector3.new(0, 6, 0)
    end
    if target.Hits >= TARGET_HITS then fadeAndDestroy(target, 0.5) end
end

-- ----------------------------------------------------------------------------
-- Bullets — neon-shaded sphere head + a single trail mesh that the shader
-- fades along its own +Z axis. One Part for the head, one for the trail.
-- ----------------------------------------------------------------------------

local BULLET_SPEED     = 80
local BULLET_MAX_RANGE = 100   -- units before the bullet despawns
local BULLET_LIFE      = BULLET_MAX_RANGE / BULLET_SPEED + 0.2  -- safety net
local TRAIL_LENGTH     = 5     -- world units of glow streak behind the bullet
local TRAIL_THICKNESS  = 0.22
local bullets = {}

-- Build a part fully configured *before* Render=true / Parent so the Unity
-- GameObject is created exactly once with the right material in place.
local function newPart(name, mat, shape, size, color, cf)
    local p = Instance.new("BasePart")
    p.Name = name
    p.Material = mat
    p.Shape = shape
    p.Size = size
    p.Color = color
    p.CFrame = cf
    p.CastShadow = false
    p.Render = true
    p.Parent = game
    return p
end

-- Source bullets: built once, parked far below the world so they're never
-- on-camera. fireBullet() clones them — the engine's Clone path captures the
-- source's GameObject as a one-shot template and Object.Instantiate's it,
-- skipping CreatePrimitive + AddComponent on every shot.
local PARKED = CFrame.new(Vector3.new(0, -10000, 0))
local bulletSource = newPart("Bullet", neonMat, "Sphere", BULLET_SIZE,
    Color3.fromRGB(255, 110, 30), PARKED)
local trailSource = newPart("BulletTrail", trailMat, "Cube",
    Vector3.new(TRAIL_THICKNESS, TRAIL_THICKNESS, TRAIL_LENGTH),
    Color3.fromRGB(255, 130, 40), PARKED)

local function fireBullet()
    -- Bullets leave the gun's muzzle and fly along the camera's sightline so
    -- aim still tracks the crosshair while the tracer reads as gun-fired.
    local fwd = cameraForward()
    local origin = gunMuzzlePos
    local cfYawPitch = CFrame.LookAt(origin, origin + fwd)

    -- Set CFrame *before* Parent so the clone's first ApplyTransform places
    -- it at the muzzle, not at the parked source position.
    local head = bulletSource:Clone()
    head.CFrame = cfYawPitch
    head.Parent = game

    -- Trail centered halfway behind the bullet, oriented so its local +Z
    -- (front of the gradient = brightest) faces the bullet.
    local trailCenter = origin - fwd * (TRAIL_LENGTH * 0.5)
    local trailCF = CFrame.LookAt(trailCenter, trailCenter + fwd)
    local trail = trailSource:Clone()
    trail.CFrame = trailCF
    trail.Parent = game

    bullets[#bullets + 1] = {
        Part = head, Trail = trail,
        Velocity = fwd * BULLET_SPEED, Direction = fwd,
        Origin = origin, Age = 0,
        LookCFrame = cfYawPitch,
    }

    addShake(0.10)
end

local function destroyBullet(b)
    if b.Part and b.Part.Parent then b.Part:Destroy() end
    if b.Trail and b.Trail.Parent then b.Trail:Destroy() end
end

local function stepBullets(dt)
    local maxRangeSq = BULLET_MAX_RANGE * BULLET_MAX_RANGE
    for i = #bullets, 1, -1 do
        local b = bullets[i]
        b.Age = b.Age + dt
        if b.Age >= BULLET_LIFE or not b.Part.Parent then
            destroyBullet(b)
            table.remove(bullets, i)
        else
            local pos = b.Part.CFrame.Position + b.Velocity * dt
            b.Part.CFrame = CFrame.fromEulerAngles(pos, b.LookCFrame.Rotation)

            -- Trail rides behind the head, same orientation.
            local trailCenter = pos - b.Direction * (TRAIL_LENGTH * 0.5)
            b.Trail.CFrame = CFrame.fromEulerAngles(trailCenter, b.LookCFrame.Rotation)

            local hit = false
            for _, target in ipairs(targets) do
                if not target.Dying and target.Part.Parent and overlaps(b.Part, target.Part) then
                    registerHit(target, b.Direction)
                    hit = true
                    break
                end
            end

            local outOfRange = (pos - b.Origin).SquaredMagnitude >= maxRangeSq
            if hit or outOfRange or pos.Y <= FLOOR_Y + BULLET_SIZE.Y * 0.5 then
                destroyBullet(b)
                table.remove(bullets, i)
            end
        end
    end
end

-- ----------------------------------------------------------------------------
-- Input
-- ----------------------------------------------------------------------------

InputService.Input:Connect(function(input, phase)
    if phase ~= "Begin" then return end

    if input.KeyCode == "Escape" then
        setPaused(not paused)
        return
    end

    if paused then return end

    if input.KeyCode == "Space" then
        local dir, moving = moveDir()
        if not moving then dir = lastMoveDir end
        playerBody.Velocity = playerBody.Velocity + dir * LUNGE_IMPULSE + Vector3.new(0, 4, 0)
        lungeVel = lungeVel + dir * LUNGE_KICK
    end
end)

mouse.ButtonDown:Connect(function(name)
    if paused then return end
    if name == "Left" then fireBullet() end
end)

-- ----------------------------------------------------------------------------
-- Main loop
-- ----------------------------------------------------------------------------

RunService.Heartbeat:Connect(function(dt)
    if paused then return end

    -- Player horizontal motion. On the ground the input maps straight to top
    -- speed (snappy, FPS-style); in the air we blend exponentially so air
    -- control exists but doesn't kill lunge momentum.
    local dir, moving = moveDir()
    if moving then
        lastMoveDir = dir
        if playerBody.OnGround then
            playerBody.Velocity = Vector3.new(dir.X * PLAYER_SPEED, playerBody.Velocity.Y, dir.Z * PLAYER_SPEED)
        else
            local k = 1 - math.exp(-AIR_CONTROL * dt)
            local tx = dir.X * PLAYER_SPEED
            local tz = dir.Z * PLAYER_SPEED
            playerBody.Velocity = Vector3.new(
                playerBody.Velocity.X + (tx - playerBody.Velocity.X) * k,
                playerBody.Velocity.Y,
                playerBody.Velocity.Z + (tz - playerBody.Velocity.Z) * k)
        end
    elseif playerBody.OnGround then
        playerBody.Velocity = Vector3.new(0, playerBody.Velocity.Y, 0)
    end

    for i = #bodies, 1, -1 do
        local b = bodies[i]
        if not b.Part or not b.Part.Parent then table.remove(bodies, i)
        else stepBody(b, dt) end
    end
    stepBullets(dt)

    if playerBody.JustLanded then addShake(0.18) end

    -- Shake decay + lunge spring integration.
    shakeMag = math.max(0, shakeMag - 1.4 * dt)
    local accel = lungeOffset * (-LUNGE_STIFFNESS) + lungeVel * (-LUNGE_DAMPING)
    lungeVel    = lungeVel    + accel * dt
    lungeOffset = lungeOffset + lungeVel * dt

    local _, right = yawBasis()
    local camFwd = cameraForward()
    local lateral = lungeOffset.X * right.X + lungeOffset.Z * right.Z
    local rollDeg = -lateral * 4

    local headPos = player.CFrame.Position + Vector3.new(0, HEAD_HEIGHT, 0)
    local shake = Vector3.new(
        (math.random() * 2 - 1) * shakeMag,
        (math.random() * 2 - 1) * shakeMag,
        (math.random() * 2 - 1) * shakeMag)

    local camPos = headPos + lungeOffset + shake
    cam.CFrame = CFrame.fromEulerAngles(camPos, Vector3.new(pitchDeg, yawDeg, rollDeg))

    -- Pose the gun: offset right/down from camera, pointing along sightline.
    -- Muzzle position is recorded so fireBullet spawns the tracer at the
    -- gun's front face rather than the player's head.
    local gunPos = camPos + right * GUN_RIGHT + Vector3.new(0, -GUN_DOWN, 0) + camFwd * GUN_FWD
    gun.CFrame = CFrame.fromEulerAngles(gunPos, Vector3.new(pitchDeg, yawDeg, rollDeg))
    gunMuzzlePos = gunPos + camFwd * (GUN_SIZE.Z * 0.5 + 0.05)
    gunMuzzleDir = camFwd

    -- Speed FOV: widen the camera with horizontal velocity. Smoothed with
    -- exponential blending so the FOV breathes instead of snapping.
    local hv = playerBody.Velocity
    local hSpeed = math.sqrt(hv.X * hv.X + hv.Z * hv.Z)
    local fovTarget = math.max(0, math.min(1,
        (hSpeed - FOV_MIN_SPEED) / (FOV_MAX_SPEED - FOV_MIN_SPEED)))
    fovT = fovT + (fovTarget - fovT) * (1 - math.exp(-5 * dt))
    cam.FOV = FOV_BASE + (FOV_MAX - FOV_BASE) * fovT
end)

RunService.BindToClose(function()
    mouse:SetLocked(false)
    mouse:SetVisible(true)
end)

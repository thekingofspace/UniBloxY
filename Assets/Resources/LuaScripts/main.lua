local input = InputService
local mouse = input.GetMouse()
local cam = game.CurrentCamera

-- Place the camera at a known position looking at the XY plane at z=0.
local camDistance = 20
cam.CFrame = CFrame.new(0, 0, -camDistance)

-- Derive playfield bounds from the camera's actual frustum at z=0.
local function refreshBounds()
	local v = cam:GetViewSize(camDistance)
	-- Slight inset so paddles aren't flush against the screen edge.
	local h = v.Y * 0.5 * 0.95
	local w = v.X * 0.5 * 0.95
	return w, h
end

local viewWidth, viewHeight = refreshBounds()

local paddleSize = Vector3.new(0.5, 2.5, 0.5)
local ballSize = Vector3.new(0.5, 0.5, 0.5)

local halfPaddleY = paddleSize.Y * 0.5
local halfPaddleX = paddleSize.X * 0.5
local halfBall = ballSize.X * 0.5

local function paddleX()
	return viewWidth - 0.5
end

local leftX = -paddleX()
local rightX = paddleX()

local playerSpeed = 14

-- Difficulty scales every round: +1 step on a player score, -1 on a player loss.
-- Each derived value lerps between an "easy" and "hard" anchor based on level.
local difficulty = 0
local minDifficulty = -3
local maxDifficulty = 6

local function diffT()
	-- Maps difficulty into 0..1 for easy linear interpolation.
	local span = maxDifficulty - minDifficulty
	return (difficulty - minDifficulty) / span
end

local function lerp(a, b, t)
	return a + (b - a) * t
end

-- Per-round parameters (recomputed whenever difficulty changes).
local aiSpeed, aiReactDelay, startSpeed, maxSpeed, speedupOnHit

local function applyDifficulty()
	local t = diffT()
	aiSpeed       = lerp(2.5,  11.0, t)   -- AI tracking responsiveness
	aiReactDelay  = lerp(0.45, 0.04, t)   -- seconds AI ignores incoming ball
	startSpeed    = lerp(5.0,  13.0, t)   -- ball speed off a reset
	maxSpeed      = lerp(14.0, 32.0, t)   -- per-rally cap
	speedupOnHit  = lerp(1.02, 1.09, t)   -- ramp inside a rally
end
applyDifficulty()

local left = Instance.New("BaseCube", game)
left.Size = paddleSize
left.CFrame = CFrame.new(leftX, 0, 0)
left.Render = true

local right = Instance.New("BaseCube", game)
right.Size = paddleSize
right.CFrame = CFrame.new(rightX, 0, 0)
right.Render = true

local ball = Instance.New("BaseCube", game)
ball.Size = ballSize
ball.CFrame = CFrame.new(0, 0, 0)
ball.Render = true

local blueShader = ShaderService.GetShader("Blue")
ball:AddShader(blueShader)
ball:SetShaderData(blueShader, "_Density", 4 / ballSize.X)

local maxLives = 3
local lives = maxLives
local aiLives = maxLives
local lifeSize = Vector3.new(0.5, 0.5, 0.5)
local lifeSpacing = 0.7
local lifeZ = 8 -- meters behind the pong plane (z=0)

local function makeLifeRow()
	local row = {}
	for i = 1, maxLives do
		local c = Instance.New("BaseCube", game)
		c.Name = "Life" .. i
		c.Size = lifeSize
		c.Render = true
		row[i] = c
	end
	return row
end

local lifeCubes = makeLifeRow()
local aiLifeCubes = makeLifeRow()

local function refreshLifeCubes()
	local v = cam:GetViewSize(camDistance + lifeZ)
	local w = v.X * 0.5 * 0.95
	local h = v.Y * 0.5 * 0.95
	local y = h - lifeSize.Y * 0.5 - 0.3

	-- Player lives: top-left, growing right.
	local leftStart = -w + lifeSize.X * 0.5 + 0.3
	for i = 1, maxLives do
		local c = lifeCubes[i]
		c.CFrame = CFrame.new(leftStart + (i - 1) * lifeSpacing, y, lifeZ)
		c.Render = i <= lives
	end

	-- AI lives: top-right, growing left.
	local rightStart = w - lifeSize.X * 0.5 - 0.3
	for i = 1, maxLives do
		local c = aiLifeCubes[i]
		c.CFrame = CFrame.new(rightStart - (i - 1) * lifeSpacing, y, lifeZ)
		c.Render = i <= aiLives
	end
end
refreshLifeCubes()

local ballPos = Vector3.new(0, 0, 0)
local ballVel = Vector3.new(startSpeed, startSpeed * 0.4, 0)
local leftY = 0
local rightY = 0

-- Match-end blink state. While blinkTogglesLeft > 0, physics is paused and
-- the ball's Render flag flips every blinkInterval seconds. We want 3 full
-- off→on cycles (6 toggles) ending visible.
local blinkTogglesLeft = 0
local blinkTimer = 0
local blinkInterval = 0.15
local pendingServeDir = 1

local function clamp(v, lo, hi)
	if v < lo then return lo end
	if v > hi then return hi end
	return v
end

local function sign(v)
	if v < 0 then return -1 end
	return 1
end

local function resetBall(dir)
	ballPos = Vector3.new(0, 0, 0)
	ballVel = Vector3.new(startSpeed * dir, startSpeed * 0.4, 0)
end

local function startMatchReset(serveDir)
	-- Hide the ball off-field and trigger the blink sequence. Physics is
	-- frozen during the blink (the heartbeat skips its main body).
	ballPos = Vector3.new(0, 0, 0)
	ballVel = Vector3.new(0, 0, 0)
	ball.Render = false
	blinkTogglesLeft = 6
	blinkTimer = 0
	pendingServeDir = serveDir
end

local function finishMatchReset()
	lives = maxLives
	aiLives = maxLives
	refreshLifeCubes()
	resetBall(pendingServeDir)
	ball.Render = true
end

local function clampPaddle(y)
	return clamp(y, -viewHeight + halfPaddleY, viewHeight - halfPaddleY)
end

RunService.Heartbeat:Connect(function(dt)
	if dt > 0.05 then dt = 0.05 end

	-- Recompute bounds every frame so window resizes / aspect changes work.
	viewWidth, viewHeight = refreshBounds()
	leftX = -paddleX()
	rightX = paddleX()
	refreshLifeCubes()

	-- Match-reset blink: pause physics, flicker the ball, then resume.
	if blinkTogglesLeft > 0 then
		blinkTimer = blinkTimer + dt
		while blinkTimer >= blinkInterval and blinkTogglesLeft > 0 do
			blinkTimer = blinkTimer - blinkInterval
			ball.Render = not ball.Render
			blinkTogglesLeft = blinkTogglesLeft - 1
		end
		left.CFrame = CFrame.new(leftX, leftY, 0)
		right.CFrame = CFrame.new(rightX, rightY, 0)
		if blinkTogglesLeft <= 0 then
			finishMatchReset()
		end
		return
	end

	-- Player paddle: W/S or Up/Down, fall back to mouse Y
	local move = 0
	if input.IsKeyDown("W") or input.IsKeyDown("Up") then move = move + 1 end
	if input.IsKeyDown("S") or input.IsKeyDown("Down") then move = move - 1 end

	if move ~= 0 then
		leftY = clampPaddle(leftY + move * playerSpeed * dt)
	else
		local size = cam:GetWindowSize()
		if size.Y > 0 then
			local targetY = (mouse.Position.Y / size.Y) * 2 * viewHeight - viewHeight
			leftY = clampPaddle(targetY)
		end
	end

	-- AI paddle: lazy, delayed chase. Only tracks while ball heads toward it,
	-- and uses a slow exponential lerp so fast/steep balls can outrun it.
	if ballVel.X > 0 then
		local timeToReach = (rightX - ballPos.X) / ballVel.X
		if timeToReach > aiReactDelay then
			rightY = clampPaddle(rightY + (ballPos.Y - rightY) * dt * aiSpeed)
		end
	end

	-- Integrate ball
	local nx = ballPos.X + ballVel.X * dt
	local ny = ballPos.Y + ballVel.Y * dt

	-- Top/bottom walls
	if ny > viewHeight - halfBall then
		ny = viewHeight - halfBall
		ballVel = Vector3.new(ballVel.X, -math.abs(ballVel.Y), 0)
	elseif ny < -viewHeight + halfBall then
		ny = -viewHeight + halfBall
		ballVel = Vector3.new(ballVel.X, math.abs(ballVel.Y), 0)
	end

	-- Left paddle collision (only when moving left)
	if ballVel.X < 0
		and nx - halfBall <= leftX + halfPaddleX
		and nx + halfBall >= leftX - halfPaddleX
		and ny <= leftY + halfPaddleY + halfBall
		and ny >= leftY - halfPaddleY - halfBall
	then
		nx = leftX + halfPaddleX + halfBall
		local offset = (ny - leftY) / (halfPaddleY + halfBall)
		local speed = math.min(math.sqrt(ballVel.X * ballVel.X + ballVel.Y * ballVel.Y) * speedupOnHit, maxSpeed)
		local angle = offset * 0.9
		ballVel = Vector3.new(math.abs(math.cos(angle)) * speed, math.sin(angle) * speed, 0)
	end

	-- Right paddle collision (only when moving right)
	if ballVel.X > 0
		and nx + halfBall >= rightX - halfPaddleX
		and nx - halfBall <= rightX + halfPaddleX
		and ny <= rightY + halfPaddleY + halfBall
		and ny >= rightY - halfPaddleY - halfBall
	then
		nx = rightX - halfPaddleX - halfBall
		local offset = (ny - rightY) / (halfPaddleY + halfBall)
		local speed = math.min(math.sqrt(ballVel.X * ballVel.X + ballVel.Y * ballVel.Y) * speedupOnHit, maxSpeed)
		local angle = offset * 0.9
		ballVel = Vector3.new(-math.abs(math.cos(angle)) * speed, math.sin(angle) * speed, 0)
	end

	-- Score: ball escaped past a paddle. Lives drive the match; difficulty
	-- only changes when a whole match ends.
	if nx < -viewWidth - 1 then
		lives = lives - 1
		refreshLifeCubes()
		if lives <= 0 then
			-- Player lost the match → AI gets weaker for the next one.
			difficulty = math.max(minDifficulty, difficulty - 1)
			applyDifficulty()
			startMatchReset(1)
			return
		end
		resetBall(1)
		nx, ny = 0, 0
	elseif nx > viewWidth + 1 then
		aiLives = aiLives - 1
		refreshLifeCubes()
		if aiLives <= 0 then
			-- Player won the match → AI gets faster, sharper, meaner.
			difficulty = math.min(maxDifficulty, difficulty + 1)
			applyDifficulty()
			startMatchReset(-1)
			return
		end
		resetBall(-1)
		nx, ny = 0, 0
	end

	ballPos = Vector3.new(nx, ny, 0)

	left.CFrame = CFrame.new(leftX, leftY, 0)
	right.CFrame = CFrame.new(rightX, rightY, 0)
	ball.CFrame = CFrame.new(ballPos.X, ballPos.Y, 0)
end)

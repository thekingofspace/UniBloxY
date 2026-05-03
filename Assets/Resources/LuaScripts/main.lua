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
local aiSpeed = 4
local aiReactDelay = 0.18
local startSpeed = 8
local maxSpeed = 20
local speedupOnHit = 1.05

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

	-- Score: ball escaped past a paddle
	if nx < -viewWidth - 1 then
		lives = lives - 1
		refreshLifeCubes()
		if lives <= 0 then
			RunService.Close()
			return
		end
		resetBall(1)
		nx, ny = 0, 0
	elseif nx > viewWidth + 1 then
		aiLives = aiLives - 1
		refreshLifeCubes()
		if aiLives <= 0 then
			RunService.Close()
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

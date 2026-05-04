local mouse = InputService.GetMouse()
local cam = game.CurrentCamera
if ShaderService.AssetsLoaded ~= true then
	ShaderService.AssetsFinishedLoading:Wait()
end

local camDistance = 20
cam.CFrame = CFrame.new(0, 0, -camDistance)

local function refreshBounds()
	local v = cam:GetViewSize(camDistance)
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

local difficulty = 0
local minDifficulty = -3
local maxDifficulty = 6

local function diffT()
	local span = maxDifficulty - minDifficulty
	return (difficulty - minDifficulty) / span
end

local function lerp(a, b, t)
	return a + (b - a) * t
end

local aiSpeed, aiReactDelay, startSpeed, maxSpeed, speedupOnHit

local function applyDifficulty()
	local t = diffT()
	aiSpeed       = lerp(2.5,  11.0, t)
	aiReactDelay  = lerp(0.45, 0.04, t)
	startSpeed    = lerp(5.0,  13.0, t)
	maxSpeed      = lerp(14.0, 32.0, t)
	speedupOnHit  = lerp(1.02, 1.09, t)
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



local sampleMat = ShaderService.GetMaterial("SampleCheckered")
sampleMat.TileSize = Vector2.new(2, 8)
left:AddMaterial(sampleMat)


local checker = ShaderService.GetTexture("Checker")
local rightMat = ShaderService.CreateMaterial("Default", "RightPaddleMat")
rightMat.Texture = checker
right:AddMaterial(rightMat)
rightMat.Color = Color3.fromHex("#88C0FF")

local inkMat = ShaderService.CreateMaterial("InkContrast", "BallInkMat")
inkMat.Texture = checker
inkMat.TileSize = Vector2.new(2, 2)
inkMat:Set("_Threshold", 0.5)
inkMat:Set("_Softness", 0.04)
inkMat.Color = Color3.fromHex("#FFE070")
ball:AddMaterial(inkMat)

local maxLives = 3
local lives = maxLives
local aiLives = maxLives
local lifeSize = Vector3.new(0.5, 0.5, 0.5)
local lifeSpacing = 0.7
local lifeZ = 8

local function makeHighlightMat(name, fillHex, outlineHex)
	local m = ShaderService.CreateMaterial("Highlight", name)
	m:Set("_FillColor", Color3.fromHex(fillHex))
	m:Set("_OutlineColor", Color3.fromHex(outlineHex))
	m:Set("_FillTransparency", 0.3)
	m:Set("_OutlineTransparency", 0.0)
	m:Set("_OutlineWidth", 0.05)
	return m
end

local playerLifeMat = makeHighlightMat("PlayerLifeHL", "#2090FF", "#FFFFFF")
local aiLifeMat     = makeHighlightMat("AILifeHL",     "#E03030", "#FFFFFF")

local function makeLifeRow(mat)
	local row = {}
	for i = 1, maxLives do
		local c = Instance.New("BaseCube", game)
		c.Name = "Life" .. i
		c.Size = lifeSize
		c.Render = true
		c:AddMaterial(mat)
		row[i] = { cube = c, mat = mat, lit = true }
	end
	return row
end

local lifeCubes = makeLifeRow(playerLifeMat)
local aiLifeCubes = makeLifeRow(aiLifeMat)

local function setLit(entry, lit)
	lit = not lit
	if entry.lit == lit then return end
	entry.lit = lit
	if lit then
		entry.cube:AddMaterial(entry.mat)
	else
		entry.cube:RemoveMaterial(entry.mat)
	end
end

local function refreshLifeCubes()
	local v = cam:GetViewSize(camDistance + lifeZ)
	local w = v.X * 0.5 * 0.95
	local h = v.Y * 0.5 * 0.95
	local y = h - lifeSize.Y * 0.5 - 0.3

	local leftStart = -w + lifeSize.X * 0.5 + 0.3
	for i = 1, maxLives do
		local e = lifeCubes[i]
		e.cube.CFrame = CFrame.new(leftStart + (i - 1) * lifeSpacing, y, lifeZ)
		setLit(e, i <= lives)
	end

	local rightStart = w - lifeSize.X * 0.5 - 0.3
	for i = 1, maxLives do
		local e = aiLifeCubes[i]
		e.cube.CFrame = CFrame.new(rightStart - (i - 1) * lifeSpacing, y, lifeZ)
		setLit(e, i <= aiLives)
	end
end
refreshLifeCubes()

local ballPos = Vector3.new(0, 0, 0)
local ballVel = Vector3.new(startSpeed, startSpeed * 0.4, 0)
local leftY = 0
local rightY = 0

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

	viewWidth, viewHeight = refreshBounds()
	leftX = -paddleX()
	rightX = paddleX()
	refreshLifeCubes()

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

	local move = 0

	if move ~= 0 then
		leftY = clampPaddle(leftY + move * playerSpeed * dt)
	else
		local size = cam:GetWindowSize()
		if size.Y > 0 then
			local targetY = (mouse.Position.Y / size.Y) * 2 * viewHeight - viewHeight
			leftY = clampPaddle(targetY)
		end
	end

	if ballVel.X > 0 then
		local timeToReach = (rightX - ballPos.X) / ballVel.X
		if timeToReach > aiReactDelay then
			rightY = clampPaddle(rightY + (ballPos.Y - rightY) * dt * aiSpeed)
		end
	end

	local nx = ballPos.X + ballVel.X * dt
	local ny = ballPos.Y + ballVel.Y * dt

	if ny > viewHeight - halfBall then
		ny = viewHeight - halfBall
		ballVel = Vector3.new(ballVel.X, -math.abs(ballVel.Y), 0)
	elseif ny < -viewHeight + halfBall then
		ny = -viewHeight + halfBall
		ballVel = Vector3.new(ballVel.X, math.abs(ballVel.Y), 0)
	end

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
	if nx < -viewWidth - 1 then
		lives = lives - 1
		refreshLifeCubes()
		if lives <= 0 then
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
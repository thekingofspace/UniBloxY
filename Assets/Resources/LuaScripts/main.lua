local safe_req = require

local Tests = {
	["Example"]       = "Example",
	["Globals"]       = "Globals",
	["Types"]         = "Types",
	["Equality"]      = "Equality",
	["Instance"]      = "Instance",
	["DataModel"]     = "DataModel",
	["Camera"]        = "Camera",
	["Folder"]        = "Folder",
	["BaseCube"]      = "BaseCube",
	["RenderGroup"]   = "RenderGroup",
	["Shadable"]      = "Shadable",
	["GUI"]           = "GUI",
	["StyledGUI"]     = "StyledGUI",
	["Clone"]         = "Clone",
	["Lights"]        = "Lights",
	["Lighting"]      = "Lighting",
	["AnimatedLight"] = "AnimatedLight",
	["AssetService"]  = "AssetService",
	["ShaderService"] = "ShaderService",
	["InputService"]  = "InputService",
	["ListenerService"] = "ListenerService",
	["MouseEnter"]    = "MouseEnter",
	["RunService"]    = "RunServiceTest",
	["System"]        = "System",
	["Fs"]            = "Fs",
	["Serde"]         = "Serde",
	["SerdeFormats"]  = "SerdeFormats",
}

---@param path string
---@return boolean, string
require = function(path)
	local suc, err = pcall(safe_req, path)

	if type(err) == "function" then
		suc, err = pcall(err)
	end

	return suc, err
end

local Wait = 0
local waitTill = nil
local thread = coroutine.running()

local conn = RunService.Heartbeat:Connect(function(dt)
	Wait = Wait+dt
	if waitTill ~= nil and waitTill <= Wait then
		-- Clear before resuming so a test that yields again mid-run (e.g. via
		-- yieldFrames) isn't woken up a second time by this stale deadline.
		waitTill = nil
		coroutine.resume(thread)
	end
end)

local Failed_Tests = {}

RunService.BindToClose(function()
	print("Failed Tests:\n", table.concat(Failed_Tests, "\n"))
end)

for Test, Path in pairs(Tests) do
	local suc, err = require("Tests/" .. Path)
	if not suc then
		print(string.format("Failed to load %s with error: %s", Test, err))
		table.insert(Failed_Tests, Test)
		waitTill = Wait+1
		coroutine.yield()
	else
		print(string.format("%s Passed the test!", Test))
		waitTill = Wait + ((typeof(err) == "number") and err or 0)
		coroutine.yield()
	end

	for _, item in ipairs(game:GetChildren()) do
		pcall(function()
			item:Destroy()
		end)
	end
end

conn:Disconnect()
RunService.Close()

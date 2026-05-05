local safe_req = require

local Tests = {
	["Example"] = "Example",
	["RunService"] = "RunServiceTest"
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

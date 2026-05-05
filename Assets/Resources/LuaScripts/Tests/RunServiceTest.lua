return function()
    local thread = coroutine.running()
    local elapsed = 0
    local done = false

    local conn
    conn = RunService.Heartbeat:Connect(function(dt)
        if done then return end

        elapsed = elapsed + dt
        if elapsed >= 3 then
            done = true
            conn:Disconnect()
            coroutine.resume(thread)
        end
    end)

    coroutine.yield()

    if not RunService.GetFPS then error("GetFPS Not found") end
    if not RunService.GetEnvironment then error("GetEnvironment Not found") end
    if not RunService.BindToClose then error("BindToClose Not found") end
    if not RunService.Close then error("Close Not found") end

    return 0
end
return function()
    local Thread = coroutine.running()
    local time = 0
    local conn = RunService.Heartbeat:Connect(function(dt)
        time = time + dt
        if time > 3 then
            coroutine.resume(Thread)
        end
    end)
    coroutine.yield()
    conn:Disconnect()
    if not RunService.GetFPS() then
        error("GetFPS Not found")
    end

    if not RunService.GetEnvironment() then
        error("GetEnvironment Not found")
    end

    if not RunService.BindToClose then
        error("BindToClose Not found")
    end

    if not RunService.Close then
        error("Close Not found")
    end
    return 0
end
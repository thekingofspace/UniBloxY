return function()
    if type(ListenerService) ~= "table" then error("ListenerService global missing") end
    if type(ListenerService.ListenToMouse)    ~= "function" then error("ListenToMouse missing") end
    if type(ListenerService.ListenToInstance) ~= "function" then error("ListenToInstance missing") end

    -- Dot-call (no self) must produce the same kind of Listener as colon-call.
    local dotMouse = ListenerService.ListenToMouse()
    if dotMouse.ClassName ~= "Listener" or dotMouse.Mode ~= "Mouse" then
        error("ListenerService.ListenToMouse() (dot syntax) should return a Mouse listener")
    end
    dotMouse:Destroy()

    local dotTargetCube = Instance.new("BaseCube", game)
    dotTargetCube.Render = true
    local dotInst = ListenerService.ListenToInstance(dotTargetCube)
    if dotInst.ClassName ~= "Listener" or dotInst.Mode ~= "Instance" then
        error("ListenerService.ListenToInstance(target) (dot syntax) should return an Instance listener")
    end
    dotInst:Destroy()
    dotTargetCube:Destroy()

    -- =========================================================================
    -- Mouse listener: API surface
    -- =========================================================================
    local mouseL = ListenerService:ListenToMouse()
    if mouseL.ClassName ~= "Listener" then error("Listener ClassName must be 'Listener'") end
    if mouseL.Mode ~= "Mouse" then error("Mouse listener Mode must be 'Mouse'") end
    if type(mouseL.OnEnter)     ~= "table" or type(mouseL.OnEnter.Connect)     ~= "function" then error("OnEnter signal missing") end
    if type(mouseL.OnLeave)     ~= "table" or type(mouseL.OnLeave.Connect)     ~= "function" then error("OnLeave signal missing") end
    if type(mouseL.OnActivated) ~= "table" or type(mouseL.OnActivated.Connect) ~= "function" then error("OnActivated signal missing") end
    if type(mouseL.OnRelease)   ~= "table" or type(mouseL.OnRelease.Connect)   ~= "function" then error("OnRelease signal missing") end

    -- AddTracker requires an Instance.
    local okStr = pcall(function() mouseL:AddTracker("not-an-instance") end)
    if okStr then error("AddTracker should reject non-Instance values") end

    -- AddTracker / RemoveTracker round-trip.
    local frame = Instance.new("Frame", game)
    frame.Size = UDim2.fromOffset(120, 80)
    mouseL:AddTracker(frame)
    local trackers = mouseL:GetTrackers()
    if #trackers ~= 1 or trackers[1] ~= frame then error("AddTracker did not register the Frame") end

    -- AddTracker is idempotent.
    mouseL:AddTracker(frame)
    if #mouseL:GetTrackers() ~= 1 then error("AddTracker should be idempotent") end

    -- RemoveTracker returns true on hit, false on miss.
    if mouseL:RemoveTracker(frame) ~= true then error("RemoveTracker should return true for a tracked Instance") end
    if mouseL:RemoveTracker(frame) ~= false then error("RemoveTracker should return false for an untracked Instance") end

    -- Destroy stops accepting new trackers.
    mouseL:Destroy()
    if mouseL.Destroyed ~= true then error("Listener.Destroyed should be true after Destroy") end
    mouseL:AddTracker(frame)
    if #mouseL:GetTrackers() ~= 0 then error("Destroyed listener should ignore AddTracker") end

    -- =========================================================================
    -- Instance listener: requires a target Instance
    -- =========================================================================
    local okMissing = pcall(function() ListenerService.ListenToInstance() end)
    if okMissing then error("ListenToInstance should reject missing target") end
    local okStrTarget = pcall(function() ListenerService.ListenToInstance("nope") end)
    if okStrTarget then error("ListenToInstance should reject string target") end

    local target = Instance.new("BaseCube", game)
    target.Render = true
    target.Size = Vector3.new(2, 2, 2)
    target.CFrame = CFrame.new(Vector3.new(0, 0, 0))

    local instL = ListenerService.ListenToInstance(target)
    if instL.Mode ~= "Instance" then error("Instance listener Mode must be 'Instance'") end

    -- =========================================================================
    -- 3D-vs-3D overlap fires OnEnter, then OnLeave when separated.
    -- =========================================================================
    local entered, left = 0, 0
    local lastEntered, lastLeft
    instL.OnEnter:Connect(function(t) entered = entered + 1; lastEntered = t end)
    instL.OnLeave:Connect(function(t) left = left + 1; lastLeft = t end)

    -- Place an overlapping cube, register it as a tracker, then yield a couple
    -- of frames so ListenerService.Update has a chance to detect the overlap.
    local intruder = Instance.new("BaseCube", game)
    intruder.Render = true
    intruder.Size = Vector3.new(2, 2, 2)
    intruder.CFrame = CFrame.new(Vector3.new(0.5, 0, 0))
    instL:AddTracker(intruder)

    local function yieldFrames(n)
        local thread = coroutine.running()
        local count = 0
        local conn
        conn = RunService.Heartbeat:Connect(function()
            count = count + 1
            if count >= n then
                conn:Disconnect()
                -- Skip the resume if some other handler already woke us up;
                -- resuming a non-suspended coroutine is what NREs in MoonSharp.
                if coroutine.status(thread) == "suspended" then
                    coroutine.resume(thread)
                end
            end
        end)
        coroutine.yield()
        -- If we got woken up by something else first, make sure our connection
        -- doesn't keep ticking and try to resume us later.
        if conn and conn.Connected then conn:Disconnect() end
    end

    yieldFrames(2)
    if entered ~= 1 then error("Instance listener should fire OnEnter once on overlap, got " .. tostring(entered)) end
    if lastEntered ~= intruder then error("OnEnter payload should be the tracker that started overlapping") end

    -- Move the intruder far away and confirm OnLeave fires.
    intruder.CFrame = CFrame.new(Vector3.new(50, 0, 0))
    yieldFrames(2)
    if left ~= 1 then error("Instance listener should fire OnLeave once when overlap ends, got " .. tostring(left)) end
    if lastLeft ~= intruder then error("OnLeave payload should be the tracker that stopped overlapping") end

    -- =========================================================================
    -- Cross-plane: a 2D tracker added to a 3D-target listener never matches.
    -- =========================================================================
    local crossFrame = Instance.new("Frame", game)
    crossFrame.Size = UDim2.fromOffset(40, 40)
    instL:AddTracker(crossFrame)
    yieldFrames(2)
    if entered ~= 1 then error("Cross-plane (2D tracker, 3D target) must not fire OnEnter") end

    -- =========================================================================
    -- RemoveTracker drains pending OnLeave so subscribers stay paired up.
    -- =========================================================================
    intruder.CFrame = CFrame.new(Vector3.new(0, 0, 0))
    yieldFrames(2)
    if entered ~= 2 then error("Re-overlap should fire a second OnEnter, got " .. tostring(entered)) end

    instL:RemoveTracker(intruder)
    if left ~= 2 then error("RemoveTracker should fire a synthetic OnLeave when removing while overlapping") end

    -- =========================================================================
    -- Destroy drains any remaining state.
    -- =========================================================================
    local clean = ListenerService.ListenToInstance(target)
    local cleanLeft = 0
    clean.OnLeave:Connect(function() cleanLeft = cleanLeft + 1 end)
    local rider = Instance.new("BaseCube", game)
    rider.Render = true
    rider.Size = Vector3.new(1, 1, 1)
    rider.CFrame = CFrame.new(Vector3.new(0, 0, 0))
    clean:AddTracker(rider)
    yieldFrames(2)
    clean:Destroy()
    if cleanLeft ~= 1 then error("Destroy should drain pending OnLeave for currently-overlapping trackers") end

    -- =========================================================================
    -- Tracker destroyed mid-overlap: synthesize OnLeave on next tick.
    -- =========================================================================
    local trackerDeath = ListenerService.ListenToInstance(target)
    local trackerDeathLeft = 0
    trackerDeath.OnLeave:Connect(function() trackerDeathLeft = trackerDeathLeft + 1 end)

    local doomed = Instance.new("BaseCube", game)
    doomed.Render = true
    doomed.Size = Vector3.new(1, 1, 1)
    doomed.CFrame = CFrame.new(Vector3.new(0, 0, 0))
    trackerDeath:AddTracker(doomed)
    yieldFrames(2) -- becomes hovered
    doomed:Destroy()
    yieldFrames(2)
    if trackerDeathLeft ~= 1 then
        error("Destroying a tracker while overlapping must fire OnLeave, got " .. tostring(trackerDeathLeft))
    end
    trackerDeath:Destroy()

    -- =========================================================================
    -- Tracker un-rendered (Render=false) mid-overlap fires OnLeave.
    -- =========================================================================
    local rendL = ListenerService.ListenToInstance(target)
    local rendLeft = 0
    rendL.OnLeave:Connect(function() rendLeft = rendLeft + 1 end)
    local rendCube = Instance.new("BaseCube", game)
    rendCube.Render = true
    rendCube.Size = Vector3.new(1, 1, 1)
    rendCube.CFrame = CFrame.new(Vector3.new(0, 0, 0))
    rendL:AddTracker(rendCube)
    yieldFrames(2)
    rendCube.Render = false
    yieldFrames(2)
    if rendLeft ~= 1 then error("Render=false on a hovered tracker must fire OnLeave, got " .. tostring(rendLeft)) end
    rendL:Destroy()

    -- =========================================================================
    -- Target destroyed mid-overlap: every open tracker gets an OnLeave drain.
    -- =========================================================================
    local doomedTarget = Instance.new("BaseCube", game)
    doomedTarget.Render = true
    doomedTarget.Size = Vector3.new(2, 2, 2)
    doomedTarget.CFrame = CFrame.new(Vector3.new(20, 0, 0))

    local targetDeath = ListenerService.ListenToInstance(doomedTarget)
    local targetDeathLeft = 0
    targetDeath.OnLeave:Connect(function() targetDeathLeft = targetDeathLeft + 1 end)

    local riderA = Instance.new("BaseCube", game)
    riderA.Render = true; riderA.Size = Vector3.new(1, 1, 1)
    riderA.CFrame = CFrame.new(Vector3.new(20, 0, 0))
    local riderB = Instance.new("BaseCube", game)
    riderB.Render = true; riderB.Size = Vector3.new(1, 1, 1)
    riderB.CFrame = CFrame.new(Vector3.new(20.5, 0, 0))

    targetDeath:AddTracker(riderA)
    targetDeath:AddTracker(riderB)
    yieldFrames(2) -- both become hovered

    doomedTarget:Destroy()
    yieldFrames(2)
    if targetDeathLeft ~= 2 then
        error("Destroying the target must drain OnLeave for every open tracker, got " .. tostring(targetDeathLeft))
    end
    targetDeath:Destroy()

    -- =========================================================================
    -- 2D: Visible=false on a hovered tracker fires OnLeave (mouse + instance share IsLive).
    -- =========================================================================
    local frameTarget = Instance.new("Frame", game)
    frameTarget.Size = UDim2.fromOffset(200, 200)
    frameTarget.Position = UDim2.fromOffset(0, 0)

    local visL = ListenerService.ListenToInstance(frameTarget)
    local visLeft = 0
    visL.OnLeave:Connect(function() visLeft = visLeft + 1 end)

    local frameTracker = Instance.new("Frame", game)
    frameTracker.Size = UDim2.fromOffset(40, 40)
    frameTracker.Position = UDim2.fromOffset(20, 20)
    visL:AddTracker(frameTracker)
    yieldFrames(2) -- overlapping rects → hovered

    frameTracker.Visible = false
    yieldFrames(2)
    if visLeft ~= 1 then
        error("Visible=false on a hovered 2D tracker must fire OnLeave, got " .. tostring(visLeft))
    end

    -- Hiding the target also drains.
    frameTracker.Visible = true
    yieldFrames(2) -- re-enter
    frameTarget.Visible = false
    yieldFrames(2)
    if visLeft < 2 then
        error("Visible=false on the target must drain OnLeave, got " .. tostring(visLeft))
    end

    visL:Destroy()
    frameTarget:Destroy()
    frameTracker:Destroy()

    return 0
end

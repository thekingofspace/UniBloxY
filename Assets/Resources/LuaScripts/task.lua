local task = {}

---@class TaskEntry
---@field thread thread
---@field resume_at number?
---@field args any[]
---@field n integer

---@type number
local clock = 0
---@type TaskEntry[]
local waiting = {}
---@type TaskEntry[]
local deferred = {}

local function safe_resume(thread, ...)
    local ok, err = coroutine.resume(thread, ...)
    if not ok then
        print("[task] error: " .. tostring(err))
    end
end

local function as_thread(fn)
    if type(fn) == "thread" then return fn end
    return coroutine.create(fn)
end

---Run fn (or resume thread) immediately.
---@param fn thread|function
---@return thread
function task.spawn(fn, ...)
    local thread = as_thread(fn)
    safe_resume(thread, ...)
    return thread
end

---Schedule fn (or thread) to run at the end of the current heartbeat.
---@param fn thread|function
---@return thread
function task.defer(fn, ...)
    local thread = as_thread(fn)
    deferred[#deferred + 1] = { thread = thread, args = { ... }, n = select("#", ...) }
    return thread
end

---Schedule fn (or thread) to run after `seconds` have elapsed.
---@param seconds number
---@param fn thread|function
---@return thread
function task.delay(seconds, fn, ...)
    local thread = as_thread(fn)
    waiting[#waiting + 1] = {
        thread = thread,
        resume_at = clock + seconds,
        args = { ... },
        n = select("#", ...),
    }
    return thread
end

---Yield the current coroutine for `seconds` (default 0). Returns elapsed time.
---@async
---@param seconds number?
---@return number elapsed
function task.wait(seconds)
    seconds = seconds or 0
    local thread, is_main = coroutine.running()
    assert(thread and not is_main, "task.wait must be called from within a coroutine")
    local start = clock
    waiting[#waiting + 1] = { thread = thread, resume_at = clock + seconds, args = {}, n = 0 }
    coroutine.yield()
    return clock - start
end

RunService.Heartbeat:Connect(function(dt)
    clock = clock + dt
    local pending = waiting
    waiting = {}
    for _, entry in ipairs(pending) do
        if clock >= entry.resume_at then
            safe_resume(entry.thread, table.unpack(entry.args, 1, entry.n))
        else
            waiting[#waiting + 1] = entry
        end
    end

    if #deferred > 0 then
        local to_run = deferred
        deferred = {}
        for _, entry in ipairs(to_run) do
            safe_resume(entry.thread, table.unpack(entry.args, 1, entry.n))
        end
    end
end)

return task

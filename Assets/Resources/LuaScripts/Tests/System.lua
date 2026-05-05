return function()
    local s = System

    local function assertType(name, val, t)
        if type(val) ~= t then
            error(name .. " expected " .. t .. ", got " .. type(val))
        end
    end

    assertType("clock", s.clock(), "number")
    assertType("time", s.time(), "number")
    assertType("timeMillis", s.timeMillis(), "number")

    assertType("date", s.date(), "string")
    assertType("utcDate", s.utcDate(), "string")

    local env = s.getenv("PATH")
    if env ~= nil then
        assertType("getenv", env, "string")
    end

    assertType("platform", s.platform(), "string")
    assertType("os", s.os(), "string")
    assertType("deviceName", s.deviceName(), "string")
    assertType("deviceModel", s.deviceModel(), "string")

    assertType("processorCount", s.processorCount(), "number")
    assertType("systemMemoryMB", s.systemMemoryMB(), "number")

    assertType("documentsPath", s.documentsPath(), "string")
    assertType("persistentPath", s.persistentPath(), "string")
    assertType("tempPath", s.tempPath(), "string")

    assertType("frameCount", s.frameCount(), "number")
    assertType("deltaTime", s.deltaTime(), "number")

    return 0
end
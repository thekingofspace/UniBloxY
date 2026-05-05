return function()
    local s = Serde

    if type(s.encode) ~= "function" then error("encode missing") end
    if type(s.decode) ~= "function" then error("decode missing") end
    if type(s.hash) ~= "function" then error("hash missing") end

    local data = { a = 1, b = "test", c = true }

    local encoded = s.encode("json", data, true)
    if type(encoded) ~= "string" then
        error("encode must return string")
    end

    local decoded = s.decode("json", encoded)
    if type(decoded) ~= "table" then
        error("decode must return table")
    end

    if decoded.a ~= 1 or decoded.b ~= "test" or decoded.c ~= true then
        error("decode mismatch")
    end

    local h1 = s.hash("md5", "hello")
    local h2 = s.hash("sha256", "hello")

    if type(h1) ~= "string" or h1 == "" then error("md5 hash invalid") end
    if type(h2) ~= "string" or h2 == "" then error("sha256 hash invalid") end

    if h1 == h2 then
        error("hash collision across algorithms")
    end

    return 0
end
return function()
    local s = Serde

    if type(s.compress)   ~= "function" then error("Serde.compress missing") end
    if type(s.decompress) ~= "function" then error("Serde.decompress missing") end

    -- ---- TOML ----
    local tomlIn = {
        title = "demo",
        count = 3,
        flag = true,
        list = { 1, 2, 3 },
        nested = { name = "inner", val = 42 },
    }
    local tomlEnc = s.encode("toml", tomlIn)
    if type(tomlEnc) ~= "string" or tomlEnc == "" then error("toml encode produced no output") end

    local tomlDec = s.decode("toml", tomlEnc)
    if type(tomlDec) ~= "table" then error("toml decode must return a table") end
    if tomlDec.title ~= "demo"  then error("toml decode title mismatch")  end
    if tomlDec.count ~= 3       then error("toml decode count mismatch")  end
    if tomlDec.flag ~= true     then error("toml decode flag mismatch")   end
    if type(tomlDec.list) ~= "table" or tomlDec.list[1] ~= 1 or tomlDec.list[3] ~= 3 then
        error("toml decode list mismatch")
    end
    if type(tomlDec.nested) ~= "table" or tomlDec.nested.name ~= "inner" or tomlDec.nested.val ~= 42 then
        error("toml decode nested section mismatch")
    end

    -- ---- YAML ----
    local yamlIn = {
        name = "yaml-test",
        nums = { 10, 20, 30 },
        nested = { a = 1, b = "two" },
    }
    local yamlEnc = s.encode("yaml", yamlIn)
    if type(yamlEnc) ~= "string" or yamlEnc == "" then error("yaml encode produced no output") end

    local yamlDec = s.decode("yaml", yamlEnc)
    if type(yamlDec) ~= "table" then error("yaml decode must return a table") end
    if yamlDec.name ~= "yaml-test" then error("yaml decode name mismatch") end
    if type(yamlDec.nested) ~= "table" or yamlDec.nested.a ~= 1 or yamlDec.nested.b ~= "two" then
        error("yaml decode nested mismatch")
    end

    -- ---- Compression ----
    local payload = string.rep("the quick brown fox jumps over the lazy dog\n", 50)

    for _, algo in ipairs({ "gzip", "deflate" }) do
        local compressed = s.compress(algo, payload)
        if type(compressed) ~= "string" or compressed == "" then
            error(algo .. " compress returned empty")
        end
        if #compressed >= #payload then
            error(algo .. " compressed size should be smaller than the input")
        end
        local restored = s.decompress(algo, compressed)
        if restored ~= payload then
            error(algo .. " roundtrip failed")
        end
    end

    -- Compose: encode → compress → decompress → decode.
    local roundtrip = s.decode("json", s.decompress("gzip", s.compress("gzip", s.encode("json", tomlIn))))
    if roundtrip.title ~= "demo" or roundtrip.count ~= 3 then
        error("encode→compress→decompress→decode roundtrip mismatch")
    end

    return 0
end

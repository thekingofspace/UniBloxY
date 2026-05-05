return function()
    if type(fs) ~= "table" then error("fs global missing") end
    for _, name in ipairs({ "write", "read", "append", "exists", "isFile", "isDir",
                            "mkdir", "remove", "move", "copy", "list", "size", "join" }) do
        if type(fs[name]) ~= "function" then error("fs." .. name .. " missing") end
    end

    local sandbox = "test_sandbox"
    -- Best-effort cleanup before we begin.
    pcall(function() fs.remove(sandbox) end)

    fs.mkdir(sandbox)
    if not fs.isDir(sandbox) then error("mkdir/isDir failed") end

    local file = fs.join(sandbox, "hello.txt")
    fs.write(file, "hello")
    if not fs.exists(file) then error("write/exists failed") end
    if not fs.isFile(file) then error("isFile failed") end
    if fs.read(file) ~= "hello" then error("read mismatch") end
    if fs.size(file) ~= 5 then error("size mismatch, got " .. tostring(fs.size(file))) end

    fs.append(file, " world")
    if fs.read(file) ~= "hello world" then error("append mismatch") end

    -- copy
    local copy = fs.join(sandbox, "copy.txt")
    fs.copy(file, copy)
    if fs.read(copy) ~= "hello world" then error("copy mismatch") end

    -- move
    local moved = fs.join(sandbox, "moved.txt")
    fs.move(copy, moved)
    if fs.exists(copy)  then error("move did not remove source") end
    if not fs.exists(moved) then error("move did not create target") end

    -- list
    local entries = fs.list(sandbox)
    if type(entries) ~= "table" or #entries < 2 then error("list missing entries") end

    -- read of missing file returns nil
    if fs.read(fs.join(sandbox, "nope.txt")) ~= nil then error("read missing should be nil") end

    -- size of missing file returns 0
    if fs.size(fs.join(sandbox, "nope.txt")) ~= 0 then error("size missing should be 0") end

    -- join with empty parts
    if fs.join("", "a", "", "b") ~= "a/b" then error("join with empties wrong") end

    -- cleanup
    fs.remove(sandbox)
    if fs.exists(sandbox) then error("remove(dir) did not delete tree") end

    return 0
end

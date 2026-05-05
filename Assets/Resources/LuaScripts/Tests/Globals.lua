return function()
    -- Core Lua functions exist.
    if type(print)       ~= "function" then error("print missing") end
    if type(typeof)      ~= "function" then error("typeof missing") end
    if type(require)     ~= "function" then error("require missing") end
    if type(pcall)       ~= "function" then error("pcall missing") end
    if type(coroutine)   ~= "table"    then error("coroutine missing") end
    if type(table)       ~= "table"    then error("table library missing") end
    if type(string)      ~= "table"    then error("string library missing") end
    if type(math)        ~= "table"    then error("math library missing") end

    -- Engine globals.
    if type(game)         ~= "table"    then error("game missing") end
    if type(Instance)     ~= "table"    then error("Instance missing") end
    if type(Vector2)      ~= "table"    then error("Vector2 lib missing") end
    if type(Vector3)      ~= "table"    then error("Vector3 lib missing") end
    if type(Color3)       ~= "table"    then error("Color3 lib missing") end
    if type(UDim)         ~= "table"    then error("UDim lib missing") end
    if type(UDim2)        ~= "table"    then error("UDim2 lib missing") end
    if type(CFrame)       ~= "table"    then error("CFrame lib missing") end

    -- Services exposed as globals.
    if type(RunService)    ~= "table" then error("RunService missing") end
    if type(InputService)  ~= "table" then error("InputService missing") end
    if type(AssetService)  ~= "table" then error("AssetService missing") end
    if type(ShaderService) ~= "table" then error("ShaderService missing") end
    if type(Lighting)      ~= "table" then error("Lighting missing") end
    if type(RaycastParams) ~= "table" then error("RaycastParams missing") end
    if type(Serde)         ~= "table" then error("Serde missing") end
    if type(System)        ~= "table" then error("System missing") end
    if type(fs)            ~= "table" then error("fs missing") end

    -- typeof exhaustive check.
    if typeof(true)  ~= "boolean"  then error("typeof(boolean) failed") end
    if typeof(0)     ~= "number"   then error("typeof(number) failed")  end
    if typeof("s")   ~= "string"   then error("typeof(string) failed")  end
    if typeof(nil)   ~= "nil"      then error("typeof(nil) failed")     end
    if typeof({})    ~= "table"    then error("typeof(table) failed")   end
    if typeof(print) ~= "function" then error("typeof(function) failed")end

    -- Custom-typed userdata reports its ClassName.
    if typeof(Vector3.new(0, 0, 0)) ~= "Vector3" then error("typeof Vector3 instance failed") end

    -- Instances report their ClassName via typeof.
    local f = Instance.new("Folder", game)
    if typeof(f) ~= "Folder" then error("typeof on Folder instance failed") end
    f:Destroy()

    -- require returns a non-nil value when the module exists.
    -- (Tests/Example just `return 1`, so require should give us 1.)
    local ok, val = require("Tests/Example")
    if not ok then error("require should succeed for Tests/Example: " .. tostring(val)) end
    if val ~= 1 then error("require did not return module value, got: " .. tostring(val)) end

    return 0
end

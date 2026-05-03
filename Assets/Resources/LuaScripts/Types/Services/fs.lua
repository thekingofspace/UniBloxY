---@meta

---@class fs
---@field write fun(path:string, content:string):nil
---@field append fun(path:string, content:string):nil
---@field read fun(path:string):string?
---@field exists fun(path:string):boolean
---@field isFile fun(path:string):boolean
---@field isDir fun(path:string):boolean
---@field mkdir fun(path:string):nil
---@field remove fun(path:string):nil
---@field move fun(from:string, to:string):nil
---@field copy fun(from:string, to:string):nil
---@field list fun(path:string):string[]
---@field size fun(path:string):integer
---@field join fun(...:string):string
---@field root fun():string
fs = {}

return fs

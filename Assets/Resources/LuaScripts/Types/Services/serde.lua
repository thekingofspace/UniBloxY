---@meta

---@alias SerdeFormat "json"|"toml"|"yaml"|"yml"
---@alias SerdeHashAlgo "md5"|"sha1"|"sha256"|"sha384"|"sha512"|"crc32"
---@alias SerdeCompression "gzip"|"deflate"

---@class Serde
---@field encode fun(format:SerdeFormat, data:any, pretty:boolean?):string
---@field decode fun(format:SerdeFormat, raw:string):any
---@field hash fun(algo:SerdeHashAlgo, input:string):string
---@field compress fun(algo:SerdeCompression, raw:string):string  -- returns base64
---@field decompress fun(algo:SerdeCompression, base64:string):string
Serde = {}

return Serde

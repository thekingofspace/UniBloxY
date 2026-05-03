---@meta

---@alias SerdeFormat "json"
---@alias SerdeHashAlgo "md5"|"sha1"|"sha256"|"sha384"|"sha512"|"crc32"

---@class Serde
---@field encode fun(format:SerdeFormat, data:any, pretty:boolean?):string
---@field decode fun(format:SerdeFormat, raw:string):any
---@field hash fun(algo:SerdeHashAlgo, input:string):string
Serde = {}

return Serde

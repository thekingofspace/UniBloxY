---@meta

---@class system
---@field clock fun():number
---@field time fun():integer
---@field timeMillis fun():integer
---@field date fun(format:string?):string
---@field utcDate fun(format:string?):string
---@field getenv fun(key:string):string?
---@field platform fun():string
---@field os fun():string
---@field deviceName fun():string
---@field deviceModel fun():string
---@field processorCount fun():integer
---@field systemMemoryMB fun():integer
---@field documentsPath fun():string
---@field persistentPath fun():string
---@field tempPath fun():string
---@field frameCount fun():integer
---@field deltaTime fun():number
System = {}

return System

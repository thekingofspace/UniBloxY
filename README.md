# UniBloxY

**Discord:** https://discord.gg/5D2kXbgbDh

## Overview

UniBloxY is a hybrid Lua + C# framework built in Unity. It is structured to support both low-level scripting through Lua and engine-level extensions through C#.

The project is intentionally split so contributors can work within their preferred layer without needing to understand the entire stack.

---

## Getting Started

### Lua (Low-Level / Gameplay Scripting)

If you're working with Lua:

* Navigate to:

  ```
  Assets/Resources/LuaScript/Main
  ```
* This is the primary entry point for Lua-side logic.
* Use this area to:

  * Define gameplay behavior
  * Script systems
  * Interact with exposed engine APIs

You do **not** need to modify C# to begin scripting here.

---

### C# (Engine / Systems Development)

If you're working with C#:

* Navigate to:

  ```
  Assets/Scripts
  ```
* This is where core systems and engine integrations live.

Typical responsibilities:

* Creating new services
* Extending Lua bindings (MoonSharp)
* Implementing signals, schedulers, and runtime systems
* Managing performance-critical logic

---

## Project Structure

```
Assets/
│
├── Resources/
│   └── LuaScript/
│       └── Main/        # Entry point for Lua developers
│
├── Scripts/             # Core C# systems and services
│
└── ...                  # Unity assets and supporting files
```

---

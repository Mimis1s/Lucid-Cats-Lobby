# Lucid Cats Lobby — 联机大厅 / Lobby Browser

> ⚠️ **AI slop** — This mod was developed with AI assistance.

A BepInEx mod that adds a public lobby browser to Lucid Cats. Find and join other players' rooms from the main menu.

## Features

- Browse public lobbies from other players (worldwide search)
- Shows host name and player count
- Click a room to join it
- Refresh button to re-search

## Requirements

**BepInEx 5 x64** (5.4.23.5 or newer) — required, NOT bundled with this mod.

## Installing BepInEx

1. Download **BepInEx_x64_5.4.23.5.zip** (x64 build) from:
   https://github.com/BepInEx/BepInEx/releases
2. Extract into the game folder so these sit next to `LucidCats.exe`:
   - `winhttp.dll`
   - `doorstop_config.ini`
   - `BepInEx\` folder
3. Launch the game once; verify a `BepInEx\LogOutput.log` file appears.

## Installing this mod

1. Make sure BepInEx is installed (see above).
2. Extract the `BepInEx` folder from this archive into the game folder and merge it.
   The final file should be at:
   - `BepInEx/plugins/LucidCatsLobby.dll`
3. Launch the game. A **Lobby** button appears in the main menu.

## Usage

- In the main menu, click **Lobby** to open the browser.
- Click a room row to join it.
- Click **Refresh** (bottom-left of the panel) to re-search.

## Notes

- You can only join a room while its host is still waiting in the bedroom. Once the host starts a dream (the game begins), the room is locked and joining returns "NotAllowed".
- Host names of strangers are fetched on demand; if someone set their Steam profile to friends-only, their name won't show.

---

# Lucid Cats Lobby — 联机大厅

> ⚠️ **AI slop**——本模组由 AI 辅助开发。

为 Steam 游戏《Lucid Cats》添加公开大厅浏览器的 BepInEx 模组，可在主菜单查找并加入其他玩家的房间。

## 功能

- 浏览其他玩家的公开房间（全球搜索）
- 显示房主名与人数
- 点击房间即可加入
- 刷新按钮可重新搜索

## 前置要求

**BepInEx 5 x64**（5.4.23.5 或更新）——必需前置，本模组不包含。

## 安装 BepInEx

1. 从下面地址下载 **BepInEx_x64_5.4.23.5.zip**（x64 版本）：
   https://github.com/BepInEx/BepInEx/releases
2. 解压到游戏根目录，使以下文件与 `LucidCats.exe` 同级：
   - `winhttp.dll`
   - `doorstop_config.ini`
   - `BepInEx\` 文件夹
3. 启动游戏一次；验证游戏目录出现 `BepInEx\LogOutput.log`。

## 安装本模组

1. 确保已安装 BepInEx（见上）。
2. 将本压缩包内的 `BepInEx` 文件夹解压到游戏根目录并合并。
   最终文件应位于：
   - `BepInEx/plugins/LucidCatsLobby.dll`
3. 启动游戏。主菜单会出现 **Lobby** 按钮。

## 使用

- 在主菜单点击 **Lobby** 打开浏览器。
- 点击某个房间行即可加入。
- 点击 **Refresh**（面板左下角）重新搜索。

## 说明

- 只有在房主仍停留在卧室等人时才能加入。一旦房主开始梦境（游戏开始），房间会被锁定，加入会返回 "NotAllowed"。
- 陌生人的房主名是即时请求的；若对方 Steam 隐私设为仅好友可见，则无法显示其名字。

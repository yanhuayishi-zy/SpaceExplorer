# SpaceExplorer

基于 `Unity 2022.3 + C#` 的黄道星座主题竖版射击游戏。核心不是简单刷怪，而是围绕星座闯关、神明战机养成、无尽肉鸽、档案收集和联机/排行榜做了完整玩法闭环。项目支持十三宫征战之路、机库选机与金币养成、自定义头像、成就称号、星历剧情、无尽模式三选一强化，并针对手机端做了触控操作与手游风主界面。

## 功能

- **十三宫征途**：白羊宫至双鱼宫 + 时间泰坦克洛诺斯，逐关讨伐星座 Boss，击败后解锁下一关
- **无尽模式**：关掉通关条件，按分数出波次与 Boss，死亡结算并上报排行榜
- **肉鸽强化**：无尽中分数达阈值弹出三选一升级，机体有专属强化池
- **神明战机**：宙斯、波塞冬、阿瑞斯、雅典娜、阿波罗、阿尔忒弥斯、赫尔墨斯、哈迪斯等机体，各有大招、子技能与弹幕样式
- **机库商城**：主菜单外选机、金币购买、装备强化（本地存档）
- **自定义头像**：游戏内从「图片 / 桌面 / 下载」选图，或放入自定义目录后刷新
- **档案系统**：成就、称号、头像、排行、星历五页签；成就发金币并解锁称号
- **排行榜**：仅无尽模式上报；可选隐藏完整 ID；Python Flask 服务端随仓库提供
- **联机**：WebSocket 房间 + 远端玩家同步（`Server/multiplayer_server.py`）
- **移动端**：虚拟摇杆/按键，手游风主界面闸门（首次设 ID → 开始 / 机库 / 排行）
- **本地持久化**：玩家 ID、金币、已购机体、成就、最高分等 PlayerPrefs + 文件兜底

## 运行

### Unity 客户端

1. 用 `Unity 2022.3.62f3c1`（或同系列 LTS）打开本仓库根目录
2. 打开场景 `Assets/Scenes/SampleScene.unity`
3. 点击 Play 即可进入手游风主界面

> Windows 上若用命令行构建，可用 Unity 安装目录下的 `Unity.exe -projectPath D:\unity\SpaceExplorer`

### 排行榜 / 联机服务端

```bash
cd Server
pip install -r requirements.txt
python leaderboard_server.py
# 可选：联机
python multiplayer_server.py
```

默认监听 `0.0.0.0:8080`。客户端排行榜地址填 `http://你的公网IP:8080`。

如果在 Windows PowerShell 里遇到执行策略拦截，可改用完整 Python 路径或 `py -3 leaderboard_server.py`。

## 操作说明

- **移动**：`WASD` / 方向键，手机端虚拟摇杆
- **射击**：自动开火（按机体射速）
- **大招**：充能满后触发（机体专属技能）
- **子技能**：冷却就绪后主动释放
- **暂停**：主菜单支持的暂停热键 / 界面按钮
- **档案**：主菜单 → 档案，查看成就、称号、头像、排行、星历
- **机库**：主菜单 → 机库，选机与购买

## 项目结构

```text
SpaceExplorer/
├─ Assets/
│  ├─ Resources/          # 立绘、弹幕、BGM、技能图标等
│  ├─ Prefabs/            # 子弹 / 敌机 / 道具
│  ├─ Scenes/
│  │  └─ SampleScene.unity
│  ├─ Scripts/
│  │  ├─ Enemies/         # Boss、小怪、弹幕 AI
│  │  ├─ Managers/        # 音频、波次、场景
│  │  ├─ Net/             # 排行榜客户端、联机同步
│  │  ├─ Player/          # 移动、射击、血量
│  │  ├─ Systems/         # 关卡数据、机体、成就、肉鸽、档案
│  │  └─ UI/              # 主界面、机库、HUD、本地选图
│  └─ WebGLTemplates/
├─ Packages/
├─ ProjectSettings/
├─ Server/
│  ├─ leaderboard_server.py
│  ├─ multiplayer_server.py
│  └─ requirements.txt
└─ README.md
```

## 玩法要点

| 模式 | 目标 | 结算 |
|------|------|------|
| 十三宫征途 | 逐关击败星座 Boss | 通关发成就/称号/金币 |
| 无尽模式 | 尽量撑波次、刷高分 | 死亡弹结算并上报排行 |

## Related

同账号下还有基于 `Vue 3 + Vite + Canvas 2D` 的贪吃蛇项目 [my-game](https://github.com/yanhuayishi-zy/my-game)，同样支持自定义头像、排行榜与移动端操作，文档风格与本仓库对齐。

## License

MIT

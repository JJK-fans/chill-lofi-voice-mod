# Chill Voice Mod — BepInEx 语音替换插件

用 RVC 转换后的 Hiro 语音替换《Chill with You Lo-Fi Story》游戏内角色语音。

### 工作流程

```
游戏启动 → VoiceManager.InitAsync()
    → _audioClipDict = {}（空字典，用到才加载）
    
插件 Awake()
    → AssetBundle.LoadFromFile("voice_assets_all")
    → 加载 1247 个 AudioClip
    
插件 PopulateDictWhenReady()
    → 等待 VoiceManager 单例可用
    → 反射获取 _audioClipDict 字段
    → 添加所有自定义 AudioClip（clip.name 作为 key）
    
游戏调用 Play("Voice_GameStart_2_001")
    → VoiceManager 查 _audioClipDict["Voice_GameStart_2_001"]
    → 找到我们的自定义 AudioClip ✓
    → 播放 Hiro 语音
```

## 目录结构

```
chill-lofi-voice-mod-1.0/
├── src/
│   ├── ChillVoiceModPlugin.cs    # 插件源码（核心）
│   ├── Cavi.ChillVoiceMod.csproj # 项目文件
│   └── bin/                      # 编译输出
├── bundle/
│   └── voice_assets_all          # 自定义语音 AssetBundle（需用 Unity 工具构建）
├── makefile                      # 构建脚本
└── README.md                     # 本文件
```

## Unity 工具

两个 Editor 脚本（放入 Unity 项目的 `Assets/Editor/`）：

### VoiceModToolkit.cs — 构建语音 Bundle

路径：`Assets/Editor/[Unity] VoiceModToolkit.cs`

菜单：**Tools → Chill Voice Modding Tool**

用法：
1. **Hiro Voice Folder** — 选择 WAV 源文件夹（必须在 `Assets/` 内）
2. 勾选 **Disable Compression** 和 **Auto-Install**
3. 点击 **Build & Install**

关键实现：
- 直接从源文件夹读取 WAV，构建时设置 `addressableNames` 匹配原游戏路径
- 每次构建前删除旧 bundle 强制重建
- 自动安装到 `BepInEx/plugins/ChillVoiceMod/voice_assets_all`

### BundleInspector.cs — 调试工具

路径：`Assets/Editor/[Unity] BundleInspector.cs`

菜单：**Tools → Inspect Voice Bundle**

用于对比自定义 bundle 和原始 bundle 的 AudioClip 名称、大小、LoadAsset 结果。

## 音频转换流水线

```
原始游戏 OGG (48kHz)
    → RVC 语音转换
    → 导出 WAV (16-bit PCM mono 48kHz)
    → Unity 导入并构建 AssetBundle
    → voice_assets_all
```

## 部署

1. 将 `JJK.ChillVoiceMod.dll` 放入 `BepInEx/plugins/ChillVoiceMod/`
2. 将 `voice_assets_all` 放入同一目录
3. 确保目标目录下没有旧版 file-swap 插件的残留 DLL
4. 启动游戏

## 兼容性

- 游戏版本：Chill with You Lo-Fi Story (Unity 2022.3.62)
- BepInEx：5.4.23.4
- 不修改任何游戏文件，不依赖文件替换
- 退出游戏时无需恢复操作

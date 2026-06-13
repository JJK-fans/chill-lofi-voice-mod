# Chill Voice Mod — BepInEx 语音替换插件

用 RVC 转换后的语音替换《Chill with You Lo-Fi Story》游戏内角色语音。
这个项目是基于这个模型替换项目https://github.com/Cavibot/chill-lofi-appearance-mod/tree/main
让claude code作为参考改出来的，以至于有些文件名甚至都没改过来
但实际上语音替换和模型替换已经是两个东西了所以请不要在意文件命名问题，再让ai改我都怕出什么未知问题毕竟目前的项目是能跑的
源代码都是claudecode写的，我就把他放到这里来，实际上用到的.dll和打包mod用到的unity工具.cs我还是想放在网盘链接

使用教程:
你只需要把解包的聪音语音（路径：\Assets\App\BulbulAssets\Audio\Voice）用rvc批量变声，然后扩展名后缀弄成干净的.wav，命名和原音频文件保持一致。
再把这文件夹放进unity项目里，用工具选好这个文件夹，选好游戏根目录，打包mod即可。
插件.dll放对游戏内的位置。
(另外BepInExz怎么安装我就不说了版本在下面有写）。
然后启动游戏即可。

本人是初次使用github上传东西，如果哪里做的不对还请指出和谅解


以下内容是AI写的仅供参考



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
1. **New Voice Folder** — 选择 WAV 源文件夹（必须在 `Assets/` 内）
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
## 📜 许可协议

本项目基于 MIT 协议开源 - 详情请参阅 [LICENSE](LICENSE) 文件。

---

## ⚠️ 免责声明

本模组仅供学习与技术交流使用，严禁用于任何商业用途。

模型版权归属原作者，请勿进行二传或二次修改。

模组处于测试阶段，建议安装前备份存档。模组不会对游戏本体进行破坏性修改，如因使用本插件造成损失，作者不承担相关责任。

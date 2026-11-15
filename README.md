# RBTray 4.14 简体中文版 / Simplified Chinese

> 非官方汉化版 · 基于 [benbuck/rbtray](https://github.com/benbuck/rbtray) **v4.14**（上游最后一版，已维护模式）。
> 本仓库是该项目的 fork，**界面文字改为简体中文 + 修复「托盘关闭窗口后图标残留」，其余逻辑与官方 v4.14 一致**。
> 程序版本号保持 **4.14.0.0** 不变。

RBTray 是一个极小的 Windows 常驻工具：把任意窗口**最小化到系统托盘**，让任务栏保持干净。
本汉化版面向中文用户，托盘菜单、错误提示、「关于」对话框全部中文化，
**「关于 RBTray」里附完整使用说明**（程序无主界面，装完即可查看怎么用）。

**关键词 / Keywords**: RBTray 汉化, RBTray 中文版, 最小化到托盘, 简体中文,
minimize to tray, system tray, Chinese translation, localized, Windows 10/11, 绿色版, 便携版

---

## 直接下载就能用 / Download

到 **[Releases 页面](https://github.com/Jonah-77/rbtray/releases/latest)** 下载，两种任选：

| 资产 | 用途 |
|---|---|
| `RBTray-4.14-zh-CN-Setup.exe` | 单文件安装器，双击即装。**安装目录可自行修改**（自动检测现有安装并预填，新装可点「浏览…」选任意文件夹）、自动备份原版、开机自启自动指向所选目录、可一键还原 |
| `RBTray-4.14-zh-CN-Portable.zip` | 免安装。解压后进 `x64`（或 `x86`）目录双击 `RBTray.exe` 即用 |

两个都是 **x64 + x86 双版本**。安装器未做数字签名（官方同样未签名），
SmartScreen 若提示，点「更多信息 → 仍要运行」。

## 使用方法

- **右键**点击窗口的最小化按钮
- 按住 **Shift** 右键点击标题栏
- **Ctrl + Alt + ↓**

托盘里单击图标还原窗口；右键图标弹出中文菜单（关于 RBTray / 退出 RBTray / 关闭窗口 / 还原窗口）。
若与其他钩子类软件冲突，快捷方式加 `--no-hook`，则只保留 `Ctrl+Alt+↓`。

## 汉化范围

| 位置 | 变化 |
|---|---|
| 托盘右键菜单 4 项 | About/Exit/Restore/Close → 关于 RBTray / 退出 RBTray / 还原窗口 / 关闭窗口 |
| 运行期提示 7 条 | 创建菜单失败、已在运行中、加载 RBHook.dll 失败、安装鼠标钩子失败、注册窗口类失败、创建窗口失败、注册热键失败（含占用提示） |
| 关于对话框 | 标题、许可证说明、版权行、「确定」按钮；新增「使用说明」分组框（三种最小化方式、还原/菜单、退出、`--no-hook`、UWP 限制）；字体 Microsoft YaHei UI 9pt，版面加宽 |
| 文件版本说明 | 追加「最小化到系统托盘 · 简体中文版」 |

保留英文：作者姓名、网址、`LegalCopyright` 法律版权文本（不翻译法律文本）。

**源码改动仅两个文件**：`RBTray.cpp`（UTF-8 BOM，`cl /utf-8`）与
`RBTray.rc`（GBK + `#pragma code_page(936)`，`LANG_CHINESE/SUBLANG_CHINESE_SIMPLIFIED`）。
`RBHook.cpp` 及全部工程文件与上游 v4.14 完全一致 —— 该 DLL 内没有任何界面文字，
因此汉化不需要替换它（也避免了全局钩子 DLL 被进程占用无法覆写的问题）。

## 与官方 / 其他中文版的区别

| | 官方 v4.14 | scavin v4.15 | 本版 |
|---|---|---|---|
| 界面 | 英文 | 菜单+About 按钮中文，错误提示仍英文 | 全部中文 |
| 核心逻辑 | — | 改了最小化/还原（`SW_MINIMIZE`/`SW_RESTORE`）、升版本号、加第三方署名 | **与官方零改动** |
| 交付 | 裸 zip | 裸 zip | 安装器 + 绿色版 + 备份/还原 |

## 构建

MSVC (v14.5x) + Windows SDK 10，Unicode (`/DUNICODE /D_UNICODE`)：

```
cl /c /O2 /MT /DUNICODE /D_UNICODE /utf-8 RBTray.cpp RBHook.cpp
rc /fo RBTray.res RBTray.rc            # rc.exe 按 code_page(936) 解析
link /DLL  RBHook.obj  -> RBHook.dll    # 生成导入库
link /SUBSYSTEM:WINDOWS RBTray.obj RBTray.res RBHook.lib user32.lib shell32.lib
```

## 已知限制（沿用上游）

- UWP / 商店应用（如 WhatsApp）无法正确最小化
- 托盘最多同时挂 64 个窗口
- `Ctrl+Alt+↓` 可能被显卡「旋转屏幕」热键占用

## 许可与致谢

**GPL v2 or later**，见 `LICENSE.txt`。本仓库是 fork，保留原作者全部版权声明：
Nikolay Redko, J.D. Purcell, Benbuck Nason。汉化部分同样以 GPL 发布。
非官方版本，与上游作者无关。

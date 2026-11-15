using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;
using System.Text;

[assembly: AssemblyTitle("RBTray 4.14 简体中文版 安装程序")]
[assembly: AssemblyProduct("RBTray")]
[assembly: AssemblyVersion("4.14.0.0")]
[assembly: AssemblyFileVersion("4.14.0.0")]
[assembly: AssemblyCopyright("基于官方 v4.14 (GPL) 源码汉化，仅界面文字改为中文")]

namespace RBSetup
{
    internal static class Native
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr FindWindow(string cls, string win);
        [DllImport("user32.dll")]
        internal static extern bool PostMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
    }

    internal static class Core
    {
        private const uint WM_CLOSE = 0x0010;

        internal static string StartupDir
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.Startup); }
        }

        private static string LnkPath
        {
            get { return Path.Combine(StartupDir, "RBTray.lnk"); }
        }

        internal static byte[] Res(string name)
        {
            Assembly a = Assembly.GetExecutingAssembly();
            string actual = null;
            foreach (string n in a.GetManifestResourceNames())
            {
                if (n == name || n.EndsWith(name) || n.EndsWith("." + name))
                {
                    actual = n;
                    break;
                }
            }
            if (actual == null)
                throw new Exception("内嵌资源缺失: " + name + "\n可用资源: " +
                    string.Join(", ", a.GetManifestResourceNames()));
            using (Stream s = a.GetManifestResourceStream(actual))
            using (MemoryStream ms = new MemoryStream())
            {
                s.CopyTo(ms);
                return ms.ToArray();
            }
        }

        private static object Com()
        {
            Type t = Type.GetTypeFromProgID("WScript.Shell");
            return Activator.CreateInstance(t);
        }

        internal static string LnkTarget(string path)
        {
            object sh = Com();
            object lnk = sh.GetType().InvokeMember("CreateShortcut",
                BindingFlags.InvokeMethod, null, sh, new object[] { path });
            return (string)lnk.GetType().InvokeMember("TargetPath",
                BindingFlags.GetProperty, null, lnk, null);
        }

        private static void MakeLnk(string path, string target, string workdir)
        {
            object sh = Com();
            object lnk = sh.GetType().InvokeMember("CreateShortcut",
                BindingFlags.InvokeMethod, null, sh, new object[] { path });
            lnk.GetType().InvokeMember("TargetPath", BindingFlags.SetProperty, null, lnk, new object[] { target });
            lnk.GetType().InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, lnk, new object[] { workdir });
            lnk.GetType().InvokeMember("Save", BindingFlags.InvokeMethod, null, lnk, null);
        }

        internal static string DetectDir()
        {
            try
            {
                if (File.Exists(LnkPath))
                {
                    string t = LnkTarget(LnkPath);
                    if (!string.IsNullOrEmpty(t))
                    {
                        string d = Path.GetDirectoryName(t);
                        if (Directory.Exists(d)) return d;
                    }
                }
            }
            catch { }

            try
            {
                Process[] ps = Process.GetProcessesByName("RBTray");
                foreach (Process p in ps)
                {
                    try { return Path.GetDirectoryName(p.MainModule.FileName); }
                    catch { }
                }
            }
            catch { }

            return null;
        }

        internal static string DefaultDir()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs", "RBTray");
        }

        private static void StopRB()
        {
            IntPtr h = Native.FindWindow("RBTrayHook", "RBTrayHook");
            if (h != IntPtr.Zero) Native.PostMessage(h, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            for (int i = 0; i < 40; i++)
            {
                if (Process.GetProcessesByName("RBTray").Length == 0) return;
                Thread.Sleep(100);
            }
        }

        private static void StartRB(string dir)
        {
            ProcessStartInfo psi = new ProcessStartInfo(Path.Combine(dir, "RBTray.exe"));
            psi.WorkingDirectory = dir;
            psi.UseShellExecute = true;
            Process.Start(psi);
        }

        private static void Backup(string f)
        {
            if (File.Exists(f) && !File.Exists(f + ".bak")) File.Copy(f, f + ".bak");
        }

        internal static string Install(string chosenDir)
        {
            // chosenDir = null -> auto-detect (for self-check / silent path).
            // Otherwise the UI lets the user pick any folder, including on a
            // fresh install where nothing is detected yet.
            string detected = DetectDir();
            bool fresh = (detected == null);
            string dir = (chosenDir != null && chosenDir.Trim().Length > 0)
                       ? chosenDir.Trim() : (fresh ? DefaultDir() : detected);
            if (!Path.IsPathRooted(dir))
                return "安装目录必须是绝对路径：\r\n" + dir;
            Directory.CreateDirectory(dir);

            bool bit64 = Environment.Is64BitOperatingSystem;
            string pre = bit64 ? "rb.x64." : "rb.x86.";

            StopRB();
            string exe = Path.Combine(dir, "RBTray.exe");
            string dll = Path.Combine(dir, "RBHook.dll");
            Backup(exe);
            Backup(dll);

            // RBTray.exe carries every Chinese string. Always replace it.
            File.WriteAllBytes(exe, Res(pre + "RBTray.exe"));

            // RBHook.dll is a global mouse-hook DLL injected into dozens of
            // processes; Windows refuses to overwrite a mapped DLL. It contains
            // NO user-facing text and our rebuild is functionally identical, so
            // keep the existing one and only lay down ours when it is missing.
            string dllNote;
            if (!File.Exists(dll))
            {
                try
                {
                    File.WriteAllBytes(dll, Res(pre + "RBHook.dll"));
                    dllNote = "RBHook.dll: 已写入（原目录缺失，补齐）";
                }
                catch (Exception e)
                {
                    dllNote = "RBHook.dll: 写入失败 -> " + e.Message;
                }
            }
            else
            {
                dllNote = "RBHook.dll: 保留现有版本（钩子 DLL 无界面文字，且正被其他进程占用，无需替换）";
            }

            // (Re)create the startup shortcut when installing fresh, when it is
            // missing, or when the user chose a different folder than the one
            // currently referenced by the shortcut.
            string lnkTarget = null;
            string lnkNote;
            try { if (File.Exists(LnkPath)) lnkTarget = LnkTarget(LnkPath); }
            catch { }
            bool needLnk = fresh || !File.Exists(LnkPath) || lnkTarget == null ||
                !string.Equals(Path.GetDirectoryName(lnkTarget), dir,
                               StringComparison.OrdinalIgnoreCase);
            if (needLnk)
            {
                MakeLnk(LnkPath, exe, dir);
                lnkNote = "开机自启: 已指向新目录 " + dir;
            }
            else
                lnkNote = "开机自启: 保持原有 RBTray.lnk 不变";

            StartRB(dir);

            return "安装完成 —— 已是简体中文版\r\n" +
                   "安装目录: " + dir + "\r\n" +
                   "位数: " + (bit64 ? "64 位" : "32 位") + "\r\n" +
                   dllNote + "\r\n" +
                   "原版备份: RBTray.exe.bak / RBHook.dll.bak\r\n" +
                   lnkNote + "\r\n" +
                   "RBTray 已重新启动，右键托盘图标 → 关于 RBTray 可查看使用说明。";
        }

        internal static string Restore(string chosenDir)
        {
            string detected = DetectDir();
            string dir = (chosenDir != null && chosenDir.Trim().Length > 0)
                       ? chosenDir.Trim() : detected;
            if (dir == null)
                return "未检测到已安装的 RBTray，请手动指定安装目录。";

            StopRB();
            string exe = Path.Combine(dir, "RBTray.exe");
            string dll = Path.Combine(dir, "RBHook.dll");
            string bexe = exe + ".bak";
            string bdll = dll + ".bak";

            if (File.Exists(bexe))
            {
                if (File.Exists(exe)) File.Delete(exe);
                File.Move(bexe, exe);
                // RBHook.dll is left untouched: it may still be mapped by other
                // processes, and it contains no translatable text anyway.
                // If a .bak exists but the live DLL is gone, put it back.
                if (!File.Exists(dll) && File.Exists(bdll))
                {
                    try { File.Copy(bdll, dll); }
                    catch { }
                }
                StartRB(dir);
                return "已从 .bak 备份还原英文原版，并重新启动。\r\n" +
                       "（RBHook.dll 保持现状：无界面文字、可能被占用）\r\n" +
                       "（如需彻底卸载，请自行删除启动文件夹里的 RBTray.lnk）";
            }
            else
            {
                if (File.Exists(exe)) File.Delete(exe);
                // Leave RBHook.dll in place: mapped by other processes, no text.
                return "已删除中文版 RBTray.exe（没有可还原的原版备份）。\r\n" +
                       "RBHook.dll 保留（可能被其他进程占用）。\r\n" +
                       "开机自启快捷方式仍在，可自行删除: " + LnkPath;
            }
        }

        private static void Put(string root, string sub, string file, string res)
        {
            string d = Path.Combine(root, sub);
            Directory.CreateDirectory(d);
            File.WriteAllBytes(Path.Combine(d, file), Res(res));
        }

        internal static void ExtractAll(string dir)
        {
            Directory.CreateDirectory(dir);
            Put(dir, "x64", "RBTray.exe", "rb.x64.RBTray.exe");
            Put(dir, "x64", "RBHook.dll", "rb.x64.RBHook.dll");
            Put(dir, "x86", "RBTray.exe", "rb.x86.RBTray.exe");
            Put(dir, "x86", "RBHook.dll", "rb.x86.RBHook.dll");
        }

        // Self-check report: real embedded resource names + per-file SHA256.
        // Lets us prove the payload is intact without running the GUI.
        internal static string ExtractReport(string dir)
        {
            StringBuilder sb = new StringBuilder();
            Assembly a = Assembly.GetExecutingAssembly();
            sb.AppendLine("RBTray setup self-check");
            sb.AppendLine("resources: " + string.Join(" | ", a.GetManifestResourceNames()));
            string[] rel = { @"x64\RBTray.exe", @"x64\RBHook.dll",
                             @"x86\RBTray.exe", @"x86\RBHook.dll" };
            foreach (string r in rel)
            {
                string f = Path.Combine(dir, r);
                byte[] b = File.ReadAllBytes(f);
                using (System.Security.Cryptography.SHA256 sha =
                       System.Security.Cryptography.SHA256.Create())
                {
                    sb.AppendLine(r + "  bytes=" + b.Length + "  sha256=" +
                        BitConverter.ToString(sha.ComputeHash(b)).Replace("-", ""));
                }
            }
            return sb.ToString();
        }
    }

    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            // Diagnostic: record what arguments actually arrived. GUI-subsystem apps
            // give no console feedback, so this file is our only trace.
            try
            {
                File.WriteAllText(Path.Combine(Path.GetTempPath(), "rbsetup_args.txt"),
                    "argc=" + args.Length + " |" + string.Join("|", args) + "|");
            }
            catch { }

            Application.EnableVisualStyles();
            if (args.Length >= 2 &&
                (args[0] == "/extract" || args[0] == "-extract"))
            {
                try
                {
                    Core.ExtractAll(args[1]);
                    File.WriteAllText(Path.Combine(args[1], "_selfcheck.txt"),
                        Core.ExtractReport(args[1]));
                }
                catch (Exception e)
                {
                    try
                    {
                        File.WriteAllText(Path.Combine(Path.GetTempPath(), "rbsetup_error.txt"),
                            e.ToString());
                    }
                    catch { }
                }
                return;
            }
            Application.Run(new MainForm());
        }
    }

    internal class MainForm : Form
    {
        private TextBox log;
        private TextBox dirBox;

        public MainForm()
        {
            Text = "RBTray 4.14 · 简体中文版 安装程序";
            Font = new Font("Microsoft YaHei UI", 9f);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(600, 442);

            string dir = Core.DetectDir();
            string target = (dir == null) ? Core.DefaultDir() : dir;

            Label info = new Label();
            info.Left = 16; info.Top = 12; info.Width = 568; info.Height = 106;
            info.Text =
                "把 RBTray 4.14 替换为「简体中文版」。托盘菜单、错误提示、关于对话框全部中文化，\r\n" +
                "「关于 RBTray」里附完整使用说明。程序逻辑与官方 v4.14 一致（仅修复关闭残留图标）。\r\n" +
                "· 原版自动备份为 RBTray.exe.bak / RBHook.dll.bak，可随时还原\r\n" +
                "· 开机自启会自动指向下面所选目录，安装后立即重启 RBTray 生效\r\n" +
                "· 未数字签名（官方同样未签名），SmartScreen 提示时选「仍要运行」";
            Controls.Add(info);

            Label lb = new Label();
            lb.Text = "安装目录（可自行修改，支持新装到任意文件夹）：";
            lb.Left = 16; lb.Top = 126; lb.Width = 568; lb.Height = 18;
            Controls.Add(lb);

            dirBox = new TextBox();
            dirBox.Left = 16; dirBox.Top = 146; dirBox.Width = 478; dirBox.Height = 24;
            dirBox.Text = target;
            Controls.Add(dirBox);

            Button bBrowse = new Button();
            bBrowse.Text = "浏览…";
            bBrowse.Left = 502; bBrowse.Top = 145; bBrowse.Width = 82; bBrowse.Height = 26;
            bBrowse.Click += delegate {
                using (FolderBrowserDialog d = new FolderBrowserDialog())
                {
                    d.Description = "选择 RBTray 安装目录";
                    d.ShowNewFolderButton = true;
                    if (Directory.Exists(dirBox.Text)) d.SelectedPath = dirBox.Text;
                    if (d.ShowDialog() == DialogResult.OK) dirBox.Text = d.SelectedPath;
                }
            };
            Controls.Add(bBrowse);

            Button bInstall = new Button();
            bInstall.Text = "安装 / 升级到简体中文版";
            bInstall.Left = 16; bInstall.Top = 184; bInstall.Width = 276; bInstall.Height = 38;
            bInstall.Click += delegate { Run(true); };
            Controls.Add(bInstall);

            Button bRestore = new Button();
            bRestore.Text = "还原英文原版 / 卸载";
            bRestore.Left = 308; bRestore.Top = 184; bRestore.Width = 276; bRestore.Height = 38;
            bRestore.Click += delegate { Run(false); };
            Controls.Add(bRestore);

            log = new TextBox();
            log.Left = 16; log.Top = 234; log.Width = 568; log.Height = 192;
            log.Multiline = true;
            log.ReadOnly = true;
            log.ScrollBars = ScrollBars.Vertical;
            log.BackColor = Color.White;
            log.WordWrap = false;
            log.Text = "就绪。可先修改上面的安装目录，再点按钮。\r\n" +
                       "检测到现有安装时已自动填入其路径；全新安装可自选文件夹。\r\n";
            Controls.Add(log);
        }

        private void Run(bool install)
        {
            try
            {
                log.Text = (install ? Core.Install(dirBox.Text) : Core.Restore(dirBox.Text))
                           + "\r\n\r\n[完成]";
            }
            catch (Exception e)
            {
                log.Text = "出错:\r\n" + e;
            }
        }
    }
}

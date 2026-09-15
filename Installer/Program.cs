using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Win32;

namespace FableAstraInstaller;

internal static class Program
{
    internal const string Version = "1.2.1";
    internal static readonly string Root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FableAstra");
    internal static readonly string AppDir = Path.Combine(Root, "App");
    internal static readonly string AppExe = Path.Combine(AppDir, "FableAstra.exe");
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\FableAstra";

    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        try
        {
            if (RuntimeInformation.OSArchitecture != Architecture.Arm64)
                throw new InvalidOperationException("This installer is for Windows ARM64. Use the ARM64 computer this app was built for.");
            using var mutex = new Mutex(true, @"Local\FableAstraSetup", out bool first);
            if (!first) throw new InvalidOperationException("Another Fable + Astra installer is already running.");
            if (args.Contains("--verify-payload"))
            {
                using var payload = OpenPayload();
                var manifest = ReadManifest(payload);
                foreach (var entry in payload.Entries.Where(e => e.FullName != "manifest.json"))
                {
                    ValidateEntry(entry.FullName);
                    using var input = entry.Open();
                    var digest = Convert.ToHexString(SHA256.HashData(input));
                    if (!manifest.TryGetValue(entry.FullName, out var expected) || digest != expected)
                        throw new InvalidDataException("Payload verification failed: " + entry.FullName);
                }
                if (payload.Entries.Count != manifest.Count + 1) throw new InvalidDataException("Payload file count mismatch.");
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(Environment.ProcessPath!)!, "Payload-verification.json"), JsonSerializer.Serialize(new { passed = true, version = Version, files = manifest.Count }));
                return 0;
            }
            if (args.Contains("--quiet"))
            {
                bool startup = StartupDefault();
                Task.Run(() => Install(startup, _ => { })).GetAwaiter().GetResult();
                if (!args.Contains("--no-launch")) Launch();
                return 0;
            }
            int preview = Array.IndexOf(args, "--preview");
            if (preview >= 0 && preview + 1 < args.Length)
            {
                using var window = new SetupWindow();
                window.Show();
                Application.DoEvents();
                using var bitmap = new Bitmap(window.Width, window.Height);
                window.DrawToBitmap(bitmap, window.ClientRectangle with { Width = window.Width, Height = window.Height });
                bitmap.Save(args[preview + 1]);
                return 0;
            }
            Application.Run(new SetupWindow());
            return 0;
        }
        catch (Exception ex)
        {
            Directory.CreateDirectory(Root);
            File.WriteAllText(Path.Combine(Root, "setup-error.log"), ex.ToString());
            if (!args.Contains("--quiet") && !args.Contains("--verify-payload"))
                MessageBox.Show(ex.Message, "Fable + Astra setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }

    internal static bool StartupDefault()
    {
        if (!File.Exists(AppExe)) return true;
        using var run = Registry.CurrentUser.OpenSubKey(RunKey);
        return run?.GetValue("FableAstra") is string;
    }

    private static ZipArchive OpenPayload() => new(Assembly.GetExecutingAssembly().GetManifestResourceStream("Payload.zip") ?? throw new InvalidDataException("The app payload is missing."), ZipArchiveMode.Read);
    private static Dictionary<string, string> ReadManifest(ZipArchive zip)
    {
        using var stream = (zip.GetEntry("manifest.json") ?? throw new InvalidDataException("Payload manifest is missing.")).Open();
        return JsonSerializer.Deserialize<Dictionary<string, string>>(stream) ?? throw new InvalidDataException("Payload manifest is invalid.");
    }

    private static void ValidateEntry(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || Path.IsPathRooted(name) || name.Contains(':') || name.Split('/', '\\').Any(p => p == ".." || p == "." || p.Length == 0))
            throw new InvalidDataException("Unsafe payload path.");
    }

    private static string InsideRoot(string name)
    {
        var path = Path.GetFullPath(Path.Combine(Root, name));
        if (!path.StartsWith(Path.GetFullPath(Root) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Installation path is outside the app folder.");
        return path;
    }

    internal static void Install(bool startup, Action<string> progress)
    {
        Directory.CreateDirectory(Root);
        var staging = InsideRoot(".install-" + Guid.NewGuid().ToString("N"));
        var previous = InsideRoot(".previous-" + Guid.NewGuid().ToString("N"));
        bool movedPrevious = false, installed = false;
        try
        {
            progress("Unpacking and checking the app…");
            Directory.CreateDirectory(staging);
            using (var zip = OpenPayload())
            {
                var manifest = ReadManifest(zip);
                if (manifest.Count != zip.Entries.Count - 1) throw new InvalidDataException("Payload file count mismatch.");
                foreach (var entry in zip.Entries.Where(e => e.FullName != "manifest.json"))
                {
                    ValidateEntry(entry.FullName);
                    var destination = Path.GetFullPath(Path.Combine(staging, entry.FullName));
                    if (!destination.StartsWith(staging + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Unsafe payload destination.");
                    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                    entry.ExtractToFile(destination);
                    using var file = File.OpenRead(destination);
                    if (!manifest.TryGetValue(entry.FullName, out var expected) || Convert.ToHexString(SHA256.HashData(file)) != expected)
                        throw new InvalidDataException("The installer is damaged: " + entry.FullName);
                }
            }
            if (!File.Exists(Path.Combine(staging, "FableAstra.exe"))) throw new InvalidDataException("The app executable is missing.");
            progress("Updating Fable + Astra…");
            StopCompanions();
            if (Directory.Exists(AppDir)) { Directory.Move(InsideRoot("App"), previous); movedPrevious = true; }
            Directory.Move(staging, InsideRoot("App"));
            installed = true;
            progress("Adding your Start menu shortcut…");
            CreateShortcut();
            using (var run = Registry.CurrentUser.CreateSubKey(RunKey))
            {
                if (startup) run.SetValue("FableAstra", "\"" + AppExe + "\" --startup");
                else run.DeleteValue("FableAstra", false);
            }
            using (var source = Assembly.GetExecutingAssembly().GetManifestResourceStream("Uninstall.ps1")!)
            using (var destination = File.Create(InsideRoot("Uninstall.ps1"))) source.CopyTo(destination);
            using (var uninstall = Registry.CurrentUser.CreateSubKey(UninstallKey))
            {
                var powershell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"WindowsPowerShell\v1.0\powershell.exe");
                var command = "\"" + powershell + "\" -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"" + InsideRoot("Uninstall.ps1") + "\"";
                uninstall.SetValue("DisplayName", "Fable + Astra");
                uninstall.SetValue("DisplayVersion", Version);
                uninstall.SetValue("Publisher", "Desktop Companions");
                uninstall.SetValue("InstallLocation", AppDir);
                uninstall.SetValue("DisplayIcon", AppExe);
                uninstall.SetValue("UninstallString", command);
                uninstall.SetValue("QuietUninstallString", command + " -Quiet");
                uninstall.SetValue("NoModify", 1, RegistryValueKind.DWord);
                uninstall.SetValue("NoRepair", 1, RegistryValueKind.DWord);
            }
            File.WriteAllText(InsideRoot("installation.json"), JsonSerializer.Serialize(new { version = Version, architecture = "win-arm64", selfContained = true, startup, installedAt = DateTimeOffset.Now }));
            // Installation has committed. A leftover locked backup must not roll back a healthy app.
            if (movedPrevious)
            {
                try { Directory.Delete(previous, true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            progress("Installed. Your companions are ready.");
        }
        catch
        {
            if (installed && Directory.Exists(AppDir)) Directory.Delete(InsideRoot("App"), true);
            if (movedPrevious && Directory.Exists(previous)) Directory.Move(previous, InsideRoot("App"));
            throw;
        }
        finally
        {
            if (Directory.Exists(staging)) Directory.Delete(staging, true);
        }
    }

    private static void StopCompanions()
    {
        foreach (var process in Process.GetProcessesByName("FableAstra"))
        using (process)
        {
            string? path;
            try { path = process.MainModule?.FileName; } catch { continue; }
            if (!string.Equals(path, AppExe, StringComparison.OrdinalIgnoreCase)) continue;
            process.CloseMainWindow();
            if (!process.WaitForExit(5000)) { process.Kill(); process.WaitForExit(5000); }
            if (!process.HasExited) throw new IOException("Close Fable + Astra from the tray menu and try again.");
        }
    }

    private static void CreateShortcut()
    {
        var shortcutFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Fable + Astra.lnk");
        dynamic shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")!)!;
        dynamic shortcut = shell.CreateShortcut(shortcutFile);
        try
        {
            shortcut.TargetPath = AppExe;
            shortcut.WorkingDirectory = AppDir;
            shortcut.Description = "Fable chan and Astra chan desktop companions";
            shortcut.IconLocation = AppExe + ",0";
            shortcut.Save();
        }
        finally { Marshal.FinalReleaseComObject(shortcut); Marshal.FinalReleaseComObject(shell); }
    }

    internal static void Launch() => Process.Start(new ProcessStartInfo(AppExe) { UseShellExecute = true, WorkingDirectory = AppDir });
}

internal sealed class SetupWindow : Form
{
    private readonly CheckBox startup = new() { Text = "Start automatically when I sign in", AutoSize = true };
    private readonly Button install = new() { Text = "Install and launch", Size = new(176, 40) };
    private readonly Label status = new() { AutoSize = true, Dock = DockStyle.Fill };
    private readonly ProgressBar progress = new() { Size = new(476, 8), Visible = false, Style = ProgressBarStyle.Marquee };
    private bool busy;

    internal SetupWindow()
    {
        Text = "Fable + Astra setup";
        AutoScaleDimensions = new SizeF(96, 96);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new(560, 412);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        Font = new Font("Segoe UI", 10);
        BackColor = Color.FromArgb(249, 246, 241);
        Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath!);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 8 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int row = 0; row < 8; row++) layout.RowStyles.Add(new RowStyle(row == 5 ? SizeType.Percent : SizeType.AutoSize, row == 5 ? 100 : 0));
        Label Copy(string text, Font? font = null, int bottom = 12) => new() { Text = text, AutoSize = true, MaximumSize = new Size(500, 0), Font = font ?? Font, Margin = new Padding(0, 0, 0, bottom) };
        layout.Controls.Add(Copy("Fable + Astra", new Font("Segoe UI Semibold", 22), 0), 0, 0);
        var subtitle = Copy("A little company for your desktop", bottom: 24);
        subtitle.ForeColor = Color.FromArgb(98, 76, 116);
        layout.Controls.Add(subtitle, 0, 1);
        layout.Controls.Add(Copy("Version 1.2.1 · Windows ARM64\nIncludes the app, all 34 sprites and the .NET runtime.", bottom: 20), 0, 2);
        startup.Margin = new Padding(0, 0, 0, 14);
        startup.Checked = Program.StartupDefault();
        layout.Controls.Add(startup, 0, 3);
        var preferences = Copy("Installs for your Windows account.\nExisting preferences are kept.");
        preferences.ForeColor = Color.DimGray;
        layout.Controls.Add(preferences, 0, 4);
        layout.Controls.Add(status, 0, 5);
        progress.Dock = DockStyle.Top;
        layout.Controls.Add(progress, 0, 6);
        install.Anchor = AnchorStyles.Right;
        install.AutoSize = true;
        install.Margin = new Padding(0, 12, 0, 0);
        layout.Controls.Add(install, 0, 7);
        Controls.Add(layout);
        AcceptButton = install;
        FormClosing += (_, e) => { if (busy) e.Cancel = true; };
        install.Click += async (_, _) =>
        {
            busy = true; install.Enabled = false; startup.Enabled = false; progress.Visible = true;
            bool startAtLogin = startup.Checked;
            var updates = new Progress<string>(text => status.Text = text);
            try
            {
                await Task.Run(() => Program.Install(startAtLogin, text => ((IProgress<string>)updates).Report(text)));
                Program.Launch();
                busy = false;
                MessageBox.Show(this, "Fable + Astra are installed and running. Find their controls in the system tray.", "Ready", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
            }
            catch (Exception ex)
            {
                busy = false; install.Enabled = true; startup.Enabled = true; progress.Visible = false;
                status.Text = "Installation did not finish.";
                MessageBox.Show(this, ex.Message, "Setup could not finish", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };
    }
}

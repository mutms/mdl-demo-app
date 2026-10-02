using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Text;
using System.Text.Json;

namespace MDL_Demo;

// Everything the app knows about WSL containers goes through the wslc CLI,
// exactly like launcher/mdl-demo.cmd does.
//
// A demo is a container named mdl-demo-NNNN: NNNN is the host port of its
// console and NNNN+1 the port of its Moodle site. Inside the container the
// ports are fixed (8081 console, 8082 site) and MDL_DEMO_PORT tells the
// console its own address.
public static class Wslc
{
    public const string Repo = "ghcr.io/mutms/mdl-demo";
    public const string Latest = "latest";
    public const string Prefix = "mdl-demo-";
    public const int DefaultPort = 8081;

    public static string DefaultImage =>
        Environment.GetEnvironmentVariable("MDL_DEMO_IMAGE") is { Length: > 0 } image ? image : $"{Repo}:{Latest}";

    const string InstallPage = "https://learn.microsoft.com/windows/wsl/install";
    const string ContainersPage = "https://aka.ms/wslc";

    // --no-distribution: containers need no Linux distribution, and installing
    // one would ask for a Linux user name.
    static WslcProblem NotInstalled => new(Lang.WslNotInstalled, InstallPage, Lang.HowToInstallWsl);

    // WSL containers came with WSL 2.9.3.
    static WslcProblem TooOld => new(Lang.WslTooOld, ContainersPage, Lang.AboutWslContainers);

    static WslcProblem NotResponding => new(Lang.WslNotResponding, ContainersPage, Lang.AboutWslContainers);

    static readonly string WslDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WSL");

    // Straight from the WSL folder: right after installing WSL, programs that
    // were already running (Explorer included) still have the old PATH.
    static string WslcExe =>
        File.Exists(Path.Combine(WslDir, "wslc.exe")) ? Path.Combine(WslDir, "wslc.exe") : "wslc";

    // Runs wslc. With a log, the command line and its output go there as they happen.
    public static async Task<WslcResult> RunAsync(IEnumerable<string> args, Action<string>? log = null)
    {
        var psi = new ProcessStartInfo(WslcExe)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var arg in args)
            psi.ArgumentList.Add(arg);
        log?.Invoke("> wslc " + string.Join(' ', psi.ArgumentList.Select(a => a.Contains(' ') ? $"\"{a}\"" : a)));

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        using var process = new Process { StartInfo = psi };
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null) return;
            lock (stdout) stdout.AppendLine(e.Data);
            if (e.Data.Length > 0) log?.Invoke(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is null) return;
            lock (stderr) stderr.AppendLine(e.Data);
            if (e.Data.Length > 0) log?.Invoke(e.Data);
        };
        process.Start();
        // Nobody can answer a prompt here: an empty stdin makes it fail fast instead of hanging.
        process.StandardInput.Close();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();
        return new WslcResult(process.ExitCode, stdout.ToString(), stderr.ToString());
    }

    static async Task RunOrThrowAsync(IEnumerable<string> args, Action<string>? log)
    {
        var result = await RunAsync(args, log);
        if (result.ExitCode != 0)
            throw new WslcException(result);
    }

    // Returns null when wslc is ready, otherwise what the user should do about it.
    public static async Task<WslcProblem?> CheckAsync()
    {
        try
        {
            var result = await RunAsync(["ps"]);
            return result.ExitCode == 0 ? null : NotResponding;
        }
        catch (Win32Exception)
        {
            // No wslc.exe. System32\wsl.exe is only a stub that ships with
            // Windows; the real WSL installs into Program Files.
            return File.Exists(Path.Combine(WslDir, "wsl.exe")) ? TooOld : NotInstalled;
        }
    }

    public static async Task<List<Demo>> ListDemosAsync()
    {
        var ps = await RunAsync(["ps", "-a", "--format", "json"]);
        if (ps.ExitCode != 0) throw new WslcException(ps);

        var demos = new List<Demo>();
        foreach (var c in ParseJson(ps.Output))
        {
            var name = Str(c, "Names");
            if (!name.StartsWith(Prefix) || !int.TryParse(name[Prefix.Length..], out var port))
                continue;
            demos.Add(new Demo
            {
                Port = port,
                State = Str(c, "State"),
                Status = Str(c, "Status"),
            });
        }
        if (demos.Count == 0) return demos;

        // The demo name and the exact image are only in inspect.
        var inspect = await RunAsync(["inspect", .. demos.Select(d => d.ContainerName)]);
        if (inspect.ExitCode == 0)
        {
            foreach (var c in ParseJson(inspect.Output))
            {
                var demo = demos.FirstOrDefault(d => "/" + d.ContainerName == Str(c, "Name"));
                if (demo is null) continue;
                demo.ImageId = ShortId(Str(c, "Image"));
                if (c.TryGetProperty("Config", out var config) && config.TryGetProperty("Env", out var env)
                    && env.ValueKind == JsonValueKind.Array)
                {
                    foreach (var e in env.EnumerateArray())
                    {
                        var s = e.GetString() ?? "";
                        if (s.StartsWith("MDL_DEMO_NAME="))
                            demo.Name = s["MDL_DEMO_NAME=".Length..];
                    }
                }
            }
        }

        // A demo created before the latest download keeps running its older image.
        var latest = (await ListImagesAsync()).FirstOrDefault(i => i.Ref == DefaultImage);
        if (latest is not null)
            foreach (var demo in demos)
                demo.IsOlderVersion = demo.ImageId.Length > 0 && demo.ImageId != latest.Id;

        return demos.OrderBy(d => d.Port).ToList();
    }

    public static async Task<List<ImageInfo>> ListImagesAsync()
    {
        var result = await RunAsync(["images", "--format", "json"]);
        if (result.ExitCode != 0) throw new WslcException(result);
        return ParseJson(result.Output)
            .Select(i => new ImageInfo(
                Str(i, "Repository"), Str(i, "Tag"), ShortId(Str(i, "ID")),
                ParseCreated(Str(i, "CreatedAt")), Str(i, "Size"), Int(i, "Containers")))
            .ToList();
    }

    public static async Task CreateAsync(int port, string? name, string image, Action<string> log)
    {
        var cname = Prefix + port;
        if ((await RunAsync(["inspect", cname])).ExitCode == 0)
            throw new InvalidOperationException(Lang.AlreadyExists(cname));

        List<string> args = ["run", "-d", "--name", cname, "-e", $"MDL_DEMO_PORT={port}"];
        if (!string.IsNullOrWhiteSpace(name))
            args.AddRange(["-e", $"MDL_DEMO_NAME={name.Trim()}"]);
        args.AddRange(["-p", $"127.0.0.1:{port}:8081", "-p", $"127.0.0.1:{port + 1}:8082", image]);
        await RunOrThrowAsync(args, log);
    }

    public static Task StartAsync(Demo demo, Action<string> log) => RunOrThrowAsync(["start", demo.ContainerName], log);

    public static Task StopAsync(Demo demo, Action<string> log) => RunOrThrowAsync(["stop", demo.ContainerName], log);

    public static async Task DeleteAsync(Demo demo, Action<string> log)
    {
        if (demo.IsRunning)
            await RunAsync(["stop", demo.ContainerName], log);
        await RunOrThrowAsync(["rm", demo.ContainerName], log);
    }

    // Existing demos keep running the image they were created from.
    public static Task PullAsync(string image, Action<string> log) => RunOrThrowAsync(["pull", image], log);

    public static async Task<bool> HasImageAsync(string image) =>
        (await ListImagesAsync()).Any(i => i.Ref == image);

    // Removes demo images no demo uses (other than "latest"), plus dangling ones.
    public static async Task<int> RemoveOldImagesAsync(IEnumerable<Demo> demos, Action<string> log)
    {
        var used = demos.Select(d => d.ImageId).ToHashSet();
        var old = (await ListImagesAsync())
            .Where(i => i.Repository == Repo && i.Tag != Latest && i.Tag != "<none>")
            .Where(i => i.Containers == 0 && !used.Contains(i.Id))
            .ToList();
        foreach (var image in old)
            await RunOrThrowAsync(["rmi", image.Ref], log);
        await RunAsync(["image", "prune", "--force"], log);
        return old.Count;
    }

    // Ports no program listens on and Windows does not reserve (Hyper-V, WSL).
    // Only looks: opening a test socket could pop up a firewall prompt.
    public static async Task<Func<int, bool>> GetPortCheckAsync()
    {
        var listening = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners()
            .Select(e => e.Port).ToHashSet();
        var reserved = new List<(int Start, int End)>();
        try
        {
            var psi = new ProcessStartInfo("netsh", "int ipv4 show excludedportrange protocol=tcp")
            {
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true,
            };
            using var netsh = Process.Start(psi)!;
            var output = await netsh.StandardOutput.ReadToEndAsync();
            await netsh.WaitForExitAsync();
            foreach (Match m in Regex.Matches(output, @"^\s*(\d+)\s+(\d+)", RegexOptions.Multiline))
                reserved.Add((int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value)));
        }
        catch (Win32Exception)
        {
        }
        return port => !listening.Contains(port) && !reserved.Any(r => port >= r.Start && port <= r.End);
    }

    // "Failed to map port '127.0.0.1:8081/tcp', ... Error code: WSAEADDRINUSE"
    public static int? PortInUse(Exception e) =>
        e is WslcException && e.Message.Contains("WSAEADDRINUSE")
        && Regex.Match(e.Message, @"127\.0\.0\.1:(\d+)") is { Success: true } m ? int.Parse(m.Groups[1].Value) : null;

    static readonly HttpClient http = new() { Timeout = TimeSpan.FromSeconds(2) };

    // The container runs before the console is listening, so wait for it to
    // answer before opening the browser. Gives up after about 30 seconds.
    public static async Task<bool> WaitForConsoleAsync(Demo demo)
    {
        for (var i = 0; i < 30; i++)
        {
            try
            {
                using var response = await http.GetAsync(demo.ConsoleUrl);
                if (response.IsSuccessStatusCode) return true;
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
            {
            }
            await Task.Delay(1000);
        }
        return false;
    }

    // wslc prints either a JSON array or one JSON object per line.
    static IEnumerable<JsonElement> ParseJson(string output)
    {
        output = output.Trim();
        if (output.Length == 0) return [];
        try
        {
            using var doc = JsonDocument.Parse(output);
            return doc.RootElement.ValueKind == JsonValueKind.Array
                ? doc.RootElement.EnumerateArray().Select(e => e.Clone()).ToList()
                : [doc.RootElement.Clone()];
        }
        catch (JsonException)
        {
            return output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(line => { using var doc = JsonDocument.Parse(line); return doc.RootElement.Clone(); })
                .ToList();
        }
    }

    static string Str(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

    static int Int(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && (v.ValueKind == JsonValueKind.Number ? v.TryGetInt32(out var n)
            : int.TryParse(v.GetString(), out n)) ? n : 0;

    static string ShortId(string id)
    {
        if (id.StartsWith("sha256:")) id = id[7..];
        return id.Length > 12 ? id[..12] : id;
    }

    // "2026-09-29 09:42:18 +0200 GMT+2"
    static DateTime? ParseCreated(string s) =>
        s.Length >= 19 && DateTime.TryParseExact(s[..19], "yyyy-MM-dd HH:mm:ss",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
}

public record WslcResult(int ExitCode, string Output, string Error);

public record WslcProblem(string Message, string HelpUrl, string HelpText);

public class WslcException(WslcResult result) : Exception(
    string.IsNullOrWhiteSpace(result.Error) ? Lang.WslcFailed(result.ExitCode) : result.Error.Trim());

public record ImageInfo(string Repository, string Tag, string Id, DateTime? Created, string Size, int Containers)
{
    public string Ref => $"{Repository}:{Tag}";
}

public class Demo
{
    public int Port { get; init; }
    public string Name { get; set; } = "";
    public string State { get; init; } = "";
    public string Status { get; init; } = "";
    public string ImageId { get; set; } = "";
    public bool IsOlderVersion { get; set; }

    public string ContainerName => Wslc.Prefix + Port;
    public bool IsRunning => State == "running";
    public bool IsStopped => !IsRunning;
    public string Title => Name.Length > 0 ? Name : $"Demo {Port}";
    public string StateText => IsRunning ? Lang.Running : Lang.Stopped;
    public string ConsoleUrl => $"http://127.0.0.1:{Port}";
}

using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace MDL_Demo;

public partial class MainWindow : Window
{
    List<Demo> demos = [];
    bool ready;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await Busy("Checking WSL containers…", ReloadAsync);
    }

    static Demo DemoOf(object sender) => (Demo)((FrameworkElement)sender).DataContext;

    void Log(string line) => Dispatcher.Invoke(() =>
    {
        LogBox.AppendText(line + Environment.NewLine);
        LogBox.ScrollToEnd();
    });

    TaskCompletionSource<bool>? dialog;
    Func<bool>? dialogValidate;

    // Shows the in-window dialog; true when the primary button was clicked.
    // The body is a message or a control (like the create form).
    Task<bool> ShowDialogAsync(string title, object body, string primary, string? close = "Cancel",
        Func<bool>? validate = null)
    {
        DialogTitle.Text = title;
        DialogBody.Content = body is string text
            ? new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap }
            : body;
        DialogPrimary.Content = primary;
        DialogClose.Content = close;
        DialogClose.Visibility = close is null ? Visibility.Collapsed : Visibility.Visible;
        Grid.SetColumnSpan(DialogPrimary, close is null ? 3 : 1);
        // Enter and Esc go to the dialog only while it is open.
        DialogPrimary.IsDefault = true;
        DialogClose.IsCancel = close is not null;
        MainContent.IsEnabled = false;
        Overlay.Visibility = Visibility.Visible;
        if (body is string) DialogPrimary.Focus();

        dialogValidate = validate;
        dialog = new TaskCompletionSource<bool>();
        return dialog.Task;
    }

    void CloseDialog(bool result)
    {
        Overlay.Visibility = Visibility.Collapsed;
        MainContent.IsEnabled = true;
        DialogPrimary.IsDefault = false;
        DialogClose.IsCancel = false;
        DialogBody.Content = null;
        dialog?.TrySetResult(result);
    }

    // A failed validation keeps the dialog open; it shows its own message.
    void DialogPrimary_Click(object sender, RoutedEventArgs e)
    {
        if (dialogValidate?.Invoke() != false) CloseDialog(true);
    }

    void DialogClose_Click(object sender, RoutedEventArgs e) => CloseDialog(false);

    // Runs one operation at a time with the buttons disabled; errors end up in a dialog.
    async Task Busy(string status, Func<Task> action)
    {
        Toolbar.IsEnabled = false;
        Cards.IsEnabled = false;
        StatusText.Text = status;
        Mouse.OverrideCursor = Cursors.AppStarting;
        try
        {
            await action();
        }
        catch (Exception e)
        {
            Log("error: " + e.Message);
            StatusText.Text = e.Message;
            Mouse.OverrideCursor = null;
            await ShowDialogAsync("Something went wrong", e.Message, "OK", close: null);
        }
        finally
        {
            Mouse.OverrideCursor = null;
            Toolbar.IsEnabled = true;
            Cards.IsEnabled = true;
            NewButton.IsEnabled = ready;
            CleanButton.IsEnabled = ready;
        }
    }

    async Task ReloadAsync()
    {
        var problem = await Wslc.CheckAsync();
        ready = problem is null;
        if (problem is not null)
        {
            WarnText.Text = problem.Message + " ";
            WarnLink.NavigateUri = new Uri(problem.HelpUrl);
            WarnLinkText.Text = problem.HelpText;
        }
        WarnPanel.Visibility = ready ? Visibility.Collapsed : Visibility.Visible;
        demos = ready ? await Wslc.ListDemosAsync() : [];
        DemoList.ItemsSource = demos;
        CreateCard.Visibility = ready ? Visibility.Visible : Visibility.Collapsed;
        StatusText.Text = "";
    }

    // A demo takes two ports, NNNN and NNNN+1: skip those of other demos
    // (stopped ones included) and those other programs use.
    async Task<int> NextFreePortAsync()
    {
        var isFree = await Wslc.GetPortCheckAsync();
        var port = Wslc.DefaultPort;
        while (demos.Any(d => Math.Abs(d.Port - port) <= 1) || !isFree(port) || !isFree(port + 1))
        {
            port += 2;
            if (port > 65534) throw new InvalidOperationException("Could not find a free port for a new demo.");
        }
        return port;
    }

    // wslc's own "Failed to map port" error is not for people.
    static Exception Friendly(Exception e, string what) => Wslc.PortInUse(e) is { } port
        ? new InvalidOperationException(
            $"{what} because port {port} is used by another program on this computer. " +
            "Close that program and try again.", e)
        : e;

    static void OpenUrl(string url) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

    async Task OpenConsoleWhenReadyAsync(Demo demo)
    {
        StatusText.Text = "Waiting for the demo to start…";
        if (await Wslc.WaitForConsoleAsync(demo))
            OpenUrl(demo.ConsoleUrl);
        else
            Log($"the demo has not answered yet - open {demo.ConsoleUrl} when it does");
    }

    async void New_Click(object sender, RoutedEventArgs e)
    {
        var image = Wslc.DefaultImage;
        var hasImage = false;
        await Busy("", async () => hasImage = await Wslc.HasImageAsync(image));

        var form = new CreateForm(demos, hasImage);
        if (!await ShowDialogAsync("New demo", form, "Create", validate: form.Validate)) return;

        var port = 0;
        await Busy("Downloading the latest version, this can take a few minutes…", async () =>
        {
            if (form.DownloadLatest || !hasImage)
            {
                try
                {
                    await Wslc.PullAsync(image, Log);
                }
                catch (Exception ex) when (hasImage)
                {
                    // Offline, most likely: the version already here will do.
                    Log($"could not download the latest version ({ex.Message}), using the one on this computer");
                }
            }

            StatusText.Text = "Creating the demo…";
            // Picked only now: the download can take minutes, other programs may start meanwhile.
            port = await NextFreePortAsync();
            try
            {
                await Wslc.CreateAsync(port, form.DemoName, image, Log);
            }
            catch (Exception ex) when (Friendly(ex, "The demo cannot be created") != ex)
            {
                throw Friendly(ex, "The demo cannot be created");
            }
            await ReloadAsync();
            var demo = demos.FirstOrDefault(d => d.Port == port);
            if (demo is not null && form.OpenConsole)
                await OpenConsoleWhenReadyAsync(demo);
            StatusText.Text = $"Created \"{demo?.Title}\". Set up your demo site in the browser.";
        });
    }

    async void Start_Click(object sender, RoutedEventArgs e)
    {
        var demo = DemoOf(sender);
        await Busy($"Starting \"{demo.Title}\"…", async () =>
        {
            try
            {
                await Wslc.StartAsync(demo, Log);
            }
            catch (Exception ex) when (Friendly(ex, $"\"{demo.Title}\" cannot start") != ex)
            {
                throw Friendly(ex, $"\"{demo.Title}\" cannot start");
            }
            await ReloadAsync();
            StatusText.Text = $"Started \"{demo.Title}\".";
        });
    }

    async void Stop_Click(object sender, RoutedEventArgs e)
    {
        var demo = DemoOf(sender);
        await Busy($"Stopping \"{demo.Title}\"…", async () =>
        {
            await Wslc.StopAsync(demo, Log);
            await ReloadAsync();
            StatusText.Text = $"Stopped \"{demo.Title}\". Its site and data are kept.";
        });
    }

    async void Delete_Click(object sender, RoutedEventArgs e)
    {
        var demo = DemoOf(sender);
        if (!await ShowDialogAsync($"Delete \"{demo.Title}\"?", "This removes its site and all its data.", "Delete"))
            return;
        await Busy($"Deleting \"{demo.Title}\"…", async () =>
        {
            await Wslc.DeleteAsync(demo, Log);
            await ReloadAsync();
            StatusText.Text = $"Deleted \"{demo.Title}\".";
        });
    }

    void Console_Click(object sender, RoutedEventArgs e) => OpenUrl(DemoOf(sender).ConsoleUrl);

    async void Clean_Click(object sender, RoutedEventArgs e)
    {
        if (!await ShowDialogAsync("Free disk space?",
                "Removes downloaded versions that no demo uses. Your demos and the latest version are kept.", "Remove"))
            return;
        await Busy("Freeing disk space…", async () =>
        {
            await Wslc.RemoveOldImagesAsync(demos, Log);
            await ReloadAsync();
            StatusText.Text = "Done.";
        });
    }

    async void Refresh_Click(object sender, RoutedEventArgs e) => await Busy("Refreshing…", ReloadAsync);

    void Link_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        OpenUrl(e.Uri.AbsoluteUri);
        e.Handled = true;
    }
}

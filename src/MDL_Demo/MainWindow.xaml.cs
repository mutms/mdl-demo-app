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
        Loaded += async (_, _) => await Busy(Lang.CheckingWsl, ReloadAsync);
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
    // The body is a message or a control (like the create form). Buttons in
    // "more" close the dialog like the close button does; the caller listens
    // to their Click to learn which one it was.
    Task<bool> ShowDialogAsync(string? title, object body, string primary, bool cancel = true,
        Func<bool>? validate = null, double width = 440, params Button[] more)
    {
        DialogFrame.MaxWidth = width;
        DialogTitle.Text = title;
        DialogTitle.Visibility = title is null ? Visibility.Collapsed : Visibility.Visible;
        DialogBody.Content = body is string text
            ? new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap }
            : body;
        DialogPrimary.Content = primary;
        DialogClose.Content = Lang.Cancel;
        DialogClose.Visibility = cancel ? Visibility.Visible : Visibility.Collapsed;
        foreach (var button in more)
        {
            button.Margin = DialogClose.Margin;
            button.HorizontalAlignment = HorizontalAlignment.Stretch;
            button.Click += DialogClose_Click;
            DialogButtons.Children.Insert(DialogButtons.Children.Count - 1, button);
        }
        // Enter and Esc go to the dialog only while it is open; without a
        // cancel button Esc goes to the primary one.
        DialogPrimary.IsDefault = true;
        DialogPrimary.IsCancel = !cancel;
        DialogClose.IsCancel = cancel;
        MainContent.IsEnabled = false;
        Overlay.Visibility = Visibility.Visible;
        // A form moves the focus to its own field once it is loaded.
        DialogPrimary.Focus();

        dialogValidate = validate;
        // The caller continues after the click is over: it may open the next
        // dialog, which could not take the focus from inside the click.
        dialog = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        return dialog.Task;
    }

    void CloseDialog(bool result)
    {
        Overlay.Visibility = Visibility.Collapsed;
        MainContent.IsEnabled = true;
        DialogPrimary.IsDefault = false;
        DialogPrimary.IsCancel = false;
        DialogClose.IsCancel = false;
        DialogBody.Content = null;
        DialogButtons.Children.RemoveRange(1, DialogButtons.Children.Count - 2);
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
            await ShowDialogAsync(Lang.SomethingWentWrong, e.Message, Lang.Ok, cancel: false);
        }
        finally
        {
            Mouse.OverrideCursor = null;
            Toolbar.IsEnabled = true;
            Cards.IsEnabled = true;
            NewButton.IsEnabled = ready;
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
            if (port > 65534) throw new InvalidOperationException(Lang.NoFreePort);
        }
        return port;
    }

    // wslc's own "Failed to map port" error is not for people.
    static Exception Friendly(Exception e, Func<int, string> message) => Wslc.PortInUse(e) is { } port
        ? new InvalidOperationException(message(port), e)
        : e;

    internal static void OpenUrl(string url) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

    async Task OpenConsoleWhenReadyAsync(Demo demo)
    {
        StatusText.Text = Lang.WaitingForDemo;
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
        if (!await ShowDialogAsync(Lang.NewDemo, form, Lang.Create, validate: form.Validate)) return;

        var port = 0;
        await Busy(Lang.Downloading, async () =>
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

            StatusText.Text = Lang.Creating;
            // Picked only now: the download can take minutes, other programs may start meanwhile.
            port = await NextFreePortAsync();
            try
            {
                await Wslc.CreateAsync(port, form.DemoName, image, Log);
            }
            catch (Exception ex) when (Friendly(ex, Lang.CannotCreatePortInUse) != ex)
            {
                throw Friendly(ex, Lang.CannotCreatePortInUse);
            }
            await ReloadAsync();
            var demo = demos.FirstOrDefault(d => d.Port == port);
            if (demo is not null && form.OpenConsole)
                await OpenConsoleWhenReadyAsync(demo);
            StatusText.Text = Lang.Created(demo?.Title);
        });
    }

    async void Start_Click(object sender, RoutedEventArgs e)
    {
        var demo = DemoOf(sender);
        await Busy(Lang.Starting(demo.Title), async () =>
        {
            try
            {
                await Wslc.StartAsync(demo, Log);
            }
            catch (Exception ex) when (Friendly(ex, port => Lang.CannotStartPortInUse(demo.Title, port)) != ex)
            {
                throw Friendly(ex, port => Lang.CannotStartPortInUse(demo.Title, port));
            }
            await ReloadAsync();
            StatusText.Text = Lang.Started(demo.Title);
        });
    }

    async void Stop_Click(object sender, RoutedEventArgs e)
    {
        var demo = DemoOf(sender);
        await Busy(Lang.Stopping(demo.Title), async () =>
        {
            await Wslc.StopAsync(demo, Log);
            await ReloadAsync();
            StatusText.Text = Lang.StoppedKept(demo.Title);
        });
    }

    async void Delete_Click(object sender, RoutedEventArgs e)
    {
        var demo = DemoOf(sender);
        if (!await ShowDialogAsync(Lang.DeleteQuestion(demo.Title), Lang.DeleteWarning, Lang.Delete))
            return;
        await Busy(Lang.Deleting(demo.Title), async () =>
        {
            await Wslc.DeleteAsync(demo, Log);
            await ReloadAsync();
            StatusText.Text = Lang.Deleted(demo.Title);
        });
    }

    void Console_Click(object sender, RoutedEventArgs e) => OpenUrl(DemoOf(sender).ConsoleUrl);

    // The brand and everything advanced live here, off the main page.
    async void About_Click(object sender, RoutedEventArgs e)
    {
        var clean = new Button
        {
            Content = Lang.FreeDiskSpaceMore,
            ToolTip = Lang.FreeDiskSpaceHint,
            IsEnabled = ready,
        };
        var language = new Button { Content = Lang.LanguageMore };
        Button? clicked = null;
        clean.Click += (_, _) => clicked = clean;
        language.Click += (_, _) => clicked = language;
        await ShowDialogAsync(null, new AboutPanel(), Lang.Close, cancel: false, width: 640, more: [clean, language]);
        if (clicked == clean) await CleanAsync();
        if (clicked == language) await PickLanguageAsync();
    }

    // For trying the translations: the choice lasts until the app closes.
    async Task PickLanguageAsync()
    {
        var options = Lang.Names
            .Select((name, i) => new RadioButton { Content = name, IsChecked = i == Lang.Current })
            .ToList();
        var panel = new StackPanel();
        panel.Children.Add(new TextBlock
        {
            Text = Lang.TestLanguageText, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 12),
        });
        foreach (var option in options) panel.Children.Add(option);
        if (!await ShowDialogAsync(Lang.TestLanguage, panel, Lang.Switch)) return;

        var picked = options.FindIndex(o => o.IsChecked == true);
        if (picked == Lang.Current) return;
        Lang.Current = picked;
        // A window reads its texts when it is built: put a new one in this one's place.
        var bounds = RestoreBounds;
        var window = new MainWindow
        {
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = bounds.Left, Top = bounds.Top, Width = bounds.Width, Height = bounds.Height,
            WindowState = WindowState,
        };
        Application.Current.MainWindow = window;
        window.Show();
        Close();
    }

    async Task CleanAsync()
    {
        if (!await ShowDialogAsync(Lang.FreeDiskSpaceQuestion, Lang.FreeDiskSpaceText, Lang.Remove)) return;
        await Busy(Lang.FreeingDiskSpace, async () =>
        {
            await Wslc.RemoveOldImagesAsync(demos, Log);
            await ReloadAsync();
            StatusText.Text = Lang.Done;
        });
    }

    async void Refresh_Click(object sender, RoutedEventArgs e) => await Busy(Lang.Refreshing, ReloadAsync);

    void Link_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        OpenUrl(e.Uri.AbsoluteUri);
        e.Handled = true;
    }
}

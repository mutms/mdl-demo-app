using System.Windows;
using System.Windows.Controls;

namespace MDL_Demo;

public partial class CreateForm : UserControl
{
    readonly List<Demo> demos;

    public string DemoName => NameBox.Text.Trim();
    public bool DownloadLatest => DownloadBox.IsChecked == true;
    public bool OpenConsole => OpenBox.IsChecked == true;

    // Without a downloaded image there is nothing to choose: it gets downloaded.
    public CreateForm(List<Demo> demos, bool hasImage)
    {
        InitializeComponent();
        this.demos = demos;
        if (!hasImage)
        {
            DownloadBox.IsEnabled = false;
            DownloadHint.Text = "The first demo downloads it, this can take a few minutes.";
        }
        Loaded += (_, _) => NameBox.Focus();
    }

    // Two cards with the same name invite stopping or deleting the wrong one.
    // Unnamed demos are fine: their titles ("Demo 8081") differ anyway.
    public bool Validate()
    {
        var clash = DemoName.Length > 0
            ? demos.FirstOrDefault(d => string.Equals(d.Name.Trim(), DemoName, StringComparison.CurrentCultureIgnoreCase))
            : null;
        if (clash is null) return true;

        NameError.Text = $"You already have a demo called \"{clash.Name.Trim()}\". Pick another name.";
        NameError.Visibility = Visibility.Visible;
        NameBox.Focus();
        NameBox.SelectAll();
        return false;
    }

    void NameBox_TextChanged(object sender, TextChangedEventArgs e) => NameError.Visibility = Visibility.Collapsed;
}

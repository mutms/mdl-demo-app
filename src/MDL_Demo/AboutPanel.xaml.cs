using System.Reflection;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace MDL_Demo;

public partial class AboutPanel : UserControl
{
    // The version and the copyright are written once, in the project file.
    public AboutPanel()
    {
        InitializeComponent();
        var assembly = typeof(AboutPanel).Assembly;
        VersionText.Text = Lang.Version(assembly.GetName().Version?.ToString(3));
        CopyrightText.Text = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright;
    }

    void Link_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        MainWindow.OpenUrl(e.Uri.AbsoluteUri);
        e.Handled = true;
    }
}

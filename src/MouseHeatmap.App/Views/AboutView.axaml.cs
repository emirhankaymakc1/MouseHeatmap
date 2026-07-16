using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace MouseHeatmap.App.Views;

public partial class AboutView : UserControl
{
    public AboutView() => AvaloniaXamlLoader.Load(this);

    private void OpenLink(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string url)
        {
            OpenBrowser(url);
        }
    }

    private static void OpenBrowser(string url)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
        }
        catch
        {
        }
    }
}

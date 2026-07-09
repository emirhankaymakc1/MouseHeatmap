using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MouseHeatmap.App.Views;

public partial class LiveView : UserControl
{
    public LiveView() => AvaloniaXamlLoader.Load(this);
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ReceiptPDFBuilder.Views
{
    public partial class MainView : UserControl
    {
        public MainView()
        {
            this.InitializeComponent();
            LogBlock.PropertyChanged += LogBlock_PropertyChanged;
        }

        private void LogBlock_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            LogScrollView.ScrollToEnd();
        }
    }
}

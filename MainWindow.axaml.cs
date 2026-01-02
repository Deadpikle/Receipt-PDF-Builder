using Avalonia.Controls;
using ReceiptPDFBuilder.Interfaces;
using ReceiptPDFBuilder.ViewModels;

namespace ReceiptPDFBuilder;

public partial class MainWindow : Window, ITopLevelGrabber
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel(this);
    }

    public TopLevel GetTopLevel()
    {
        return this;
    }
}
using System.Windows;
using LocalKnowledgeBase.ViewModels;
using Wpf.Ui.Controls;

namespace LocalKnowledgeBase
{
    public partial class MainWindow : FluentWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
            
            // 应用 Mica 背景效果
            Wpf.Ui.Appearance.SystemThemeWatcher.Watch(this);
        }
    }
}

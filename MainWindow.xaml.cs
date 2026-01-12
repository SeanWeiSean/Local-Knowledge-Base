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
            
            var viewModel = new MainViewModel();
            DataContext = viewModel;
            
            // 订阅滚动事件
            viewModel.ScrollToBottomRequested += () =>
            {
                Dispatcher.InvokeAsync(() =>
                {
                    ChatScrollViewer.ScrollToEnd();
                });
            };
            
            // 应用 Mica 背景效果
            Wpf.Ui.Appearance.SystemThemeWatcher.Watch(this);
        }
    }
}

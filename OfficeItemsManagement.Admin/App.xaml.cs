using System.Windows;
using System.Windows.Threading;

namespace OfficeItemsManagement.Admin;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 全局未处理异常兜底
        DispatcherUnhandledException += (s, args) =>
        {
            MessageBox.Show($"程序异常: {args.Exception.Message}\n\n{args.Exception.StackTrace}",
                "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        // 线程池未处理异常
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            MessageBox.Show($"严重错误: {ex?.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        };
    }
}

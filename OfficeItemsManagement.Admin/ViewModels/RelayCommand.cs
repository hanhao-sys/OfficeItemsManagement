using System.Windows.Input;

namespace OfficeItemsManagement.Admin.ViewModels;

/// <summary>
/// MVVM 命令 — 将 UI 按钮点击绑定到 ViewModel 方法
/// 比 XAML 的 Click 事件更符合 MVVM 模式
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action _execute;           // 执行体
    private readonly Func<bool>? _canExecute;    // 可选：可用性判断

    /// <summary>
    /// 创建命令
    /// </summary>
    /// <param name="execute">点击时执行的委托</param>
    /// <param name="canExecute">可选：返回 false 时按钮禁用</param>
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    /// <summary>
    /// CanExecuteChanged 事件 — WPF 命令系统通过 CommandManager.RequerySuggested 触发
    /// 按钮会定期检查 CanExecute 来决定是否禁用
    /// </summary>
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    /// <summary>判断命令是否可执行</summary>
    public bool CanExecute(object? parameter) =>
        _canExecute == null || _canExecute();

    /// <summary>执行命令</summary>
    public void Execute(object? parameter) => _execute();
}

/// <summary>
/// 泛型版 RelayCommand — 支持传递参数
/// 例如 CommandParameter="{Binding SelectedItem}"
/// </summary>
/// <typeparam name="T">参数类型</typeparam>
public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) =>
        _canExecute == null || _canExecute((T?)parameter);

    public void Execute(object? parameter) => _execute((T?)parameter);
}

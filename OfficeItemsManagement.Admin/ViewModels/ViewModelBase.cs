using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OfficeItemsManagement.Admin.ViewModels;

/// <summary>
/// MVVM ViewModel 基类 — 实现 INotifyPropertyChanged
/// 所有 ViewModel 继承此类，获得属性变更通知能力
/// 
/// 使用方式：
///   class MyViewModel : ViewModelBase {
///       private string _name;
///       public string Name {
///           get => _name;
///           set => SetProperty(ref _name, value);  // 自动通知 UI 更新
///       }
///   }
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    /// <summary>
    /// 属性变更事件 — WPF 绑定系统监听此事件来刷新 UI
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 手动触发属性变更通知
    /// </summary>
    /// <param name="propertyName">属性名（CallerMemberName 自动填充）</param>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary>
    /// 设置属性值，值变化时自动通知 UI
    /// </summary>
    /// <typeparam name="T">属性类型</typeparam>
    /// <param name="field">字段引用</param>
    /// <param name="value">新值</param>
    /// <param name="propertyName">属性名（自动填充）</param>
    /// <returns>true = 值已更新并通知 UI</returns>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        // 值未变化，跳过通知
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

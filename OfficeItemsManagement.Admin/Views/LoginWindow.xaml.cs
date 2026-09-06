using System.Windows;

namespace OfficeItemsManagement.Admin.Views;

/// <summary>
/// 管理员登录窗口
/// 
/// 实验要求：
///   - 管理员账号：userid=admin, password=0000
///   - 用验证控件实现对用户名、密码不能为空的提醒
///
/// 实现方式：代码中手动检查空值并显示错误提示
/// </summary>
public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 登录按钮点击事件
    /// 验证流程：
    ///   1. 检查用户名是否为空 → 显示红色错误提示
    ///   2. 检查密码是否为空 → 显示红色错误提示
    ///   3. 匹配 admin/0000 → 跳转主窗口
    ///   4. 不匹配 → 显示"用户名或密码错误"
    /// </summary>
    private void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        // 隐藏之前的错误提示
        TxtError.Visibility = Visibility.Collapsed;

        var userId = TxtUserId.Text.Trim();
        var password = TxtPassword.Password;

        // ======== 验证控件：用户名不能为空 ========
        if (string.IsNullOrEmpty(userId))
        {
            TxtError.Text = "用户名不能为空！";
            TxtError.Visibility = Visibility.Visible;
            TxtUserId.Focus();  // 光标聚焦到输入框
            return;
        }

        // ======== 验证控件：密码不能为空 ========
        if (string.IsNullOrEmpty(password))
        {
            TxtError.Text = "密码不能为空！";
            TxtError.Visibility = Visibility.Visible;
            TxtPassword.Focus();
            return;
        }

        // ======== 验证管理员账号 ========
        if (userId == "admin" && password == "0000")
        {
            // 登录成功 → 打开主窗口，关闭登录窗口
            var mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
        else
        {
            // 登录失败
            TxtError.Text = "用户名或密码错误！";
            TxtError.Visibility = Visibility.Visible;
        }
    }

    /// <summary>取消按钮 — 退出程序</summary>
    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}

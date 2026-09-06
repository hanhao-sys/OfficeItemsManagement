using System.Windows;
using System.Windows.Controls;
using OfficeItemsManagement.Admin.Services;
using OfficeItemsManagement.Common.DTOs;

namespace OfficeItemsManagement.Admin.Views;

/// <summary>
/// 用户维护页面 — 管理员对普通用户的增删改操作
/// 
/// 数据存储：JSON 文件（Users.json）
/// 实验要求：用户信息存在文件中（JSON）
/// 
/// 操作流程：
///   左侧列表选用户 → 右侧表单编辑
///   新增按钮 → 清空表单，输入后点保存
///   保存 → 自动判断新增/修改
///   删除 → 二次确认后执行
/// </summary>
public partial class UserManagementPage : Page
{
    public UserManagementPage()
    {
        InitializeComponent();
        RefreshList();  // 页面打开时立即加载用户列表
    }

    /// <summary>
    /// 刷新用户列表 — 从 JSON 文件重新加载并绑定到 ListBox
    /// 同时清空编辑表单
    /// </summary>
    private void RefreshList()
    {
        var users = ServiceLocator.UserService.GetAllUsers();

        // 先置 null 再赋值，强制 WPF 刷新列表
        LstUsers.ItemsSource = null;
        LstUsers.ItemsSource = users;

        ClearForm();
        EditPanel.IsEnabled = false;  // 未选择用户时禁用编辑区
    }

    /// <summary>清空编辑表单，恢复默认值</summary>
    private void ClearForm()
    {
        TxtUserId.Text = "";
        TxtUserId.IsEnabled = true;    // 新增时允许输入 ID
        TxtPassword.Text = "";
        TxtName.Text = "";
        RbMale.IsChecked = true;       // 默认性别"男"
        DpBirthDate.SelectedDate = DateTime.Today;
        TxtPhone.Text = "";
    }

    /// <summary>
    /// 列表选择变更事件 — 选中用户后回填表单
    /// 编辑模式下用户 ID 不可修改（IsEnabled = false）
    /// </summary>
    private void LstUsers_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LstUsers.SelectedItem is AppUserDto user)
        {
            TxtUserId.Text = user.UserId;
            TxtUserId.IsEnabled = false;  // 编辑时不能改 ID
            TxtPassword.Text = user.Password;
            TxtName.Text = user.Name;
            RbMale.IsChecked = user.Gender == "男";
            RbFemale.IsChecked = user.Gender == "女";
            DpBirthDate.SelectedDate = user.BirthDate;
            TxtPhone.Text = user.Phone;
            EditPanel.IsEnabled = true;   // 启用编辑区
        }
    }

    /// <summary>新增按钮 — 清空表单，允许输入新用户 ID</summary>
    private void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        ClearForm();
        TxtUserId.IsEnabled = true;      // 新增时可以输入 ID
        EditPanel.IsEnabled = true;
        TxtUserId.Focus();               // 光标定位到 ID 输入框
    }

    /// <summary>
    /// 保存按钮 — 自动判断新增或修改
    /// 
    /// 判断逻辑：
    ///   - 用户 ID 已存在 且 输入框可编辑（新增模式）→ 提示 ID 重复
    ///   - 用户 ID 已存在 且 输入框不可编辑（编辑模式）→ 执行更新
    ///   - 用户 ID 不存在 → 执行新增
    ///
    /// 验证：用户 ID、密码、姓名不能为空
    /// </summary>
    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var userId = TxtUserId.Text.Trim();
            var password = TxtPassword.Text.Trim();
            var name = TxtName.Text.Trim();

            // ======== 表单验证 ========
            if (string.IsNullOrEmpty(userId))
            {
                MessageBox.Show("用户ID不能为空！", "验证",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrEmpty(password))
            {
                MessageBox.Show("密码不能为空！", "验证",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("姓名不能为空！", "验证",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 组装 DTO
            var user = new AppUserDto
            {
                UserId = userId,
                Password = password,
                Name = name,
                Gender = RbMale.IsChecked == true ? "男" : "女",
                BirthDate = DpBirthDate.SelectedDate ?? DateTime.Today,
                Phone = TxtPhone.Text.Trim()
            };

            // ======== 判断新增/修改 ========
            var existing = ServiceLocator.UserService.GetUserById(userId);

            // 新增模式下 ID 已存在 → 阻止
            if (existing != null && TxtUserId.IsEnabled)
            {
                MessageBox.Show($"用户ID '{userId}' 已存在！", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 执行新增或更新
            if (existing != null)
                ServiceLocator.UserService.UpdateUser(user);   // 编辑模式
            else
                ServiceLocator.UserService.AddUser(user);      // 新增模式

            RefreshList();
            MessageBox.Show("保存成功！", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存失败: {ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>删除按钮 — 二次确认后从 JSON 文件中删除用户</summary>
    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (LstUsers.SelectedItem is not AppUserDto user)
        {
            MessageBox.Show("请先选择要删除的用户！", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 二次确认，防止误删
        var result = MessageBox.Show(
            $"确定删除用户 '{user.UserId} - {user.Name}' 吗？",
            "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                ServiceLocator.UserService.DeleteUser(user.UserId);
                RefreshList();
                MessageBox.Show("删除成功！", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    /// <summary>刷新按钮 — 重新加载用户列表</summary>
    private void BtnRefresh_Click(object sender, RoutedEventArgs e) => RefreshList();
}

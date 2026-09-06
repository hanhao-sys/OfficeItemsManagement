using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace OfficeItemsManagement.Admin.Views;

/// <summary>
/// 管理员主窗口 — 顶部导航栏 + Frame 内容区
/// 
/// 导航结构：
///   顶部 5 个功能按钮 → 点击切换 Frame 内的 Page
///   用户维护 | 物品信息 | 物品入库 | 物品出库 | 库存查询
///
/// 底部状态栏：状态文字 + 实时时钟
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // ======== 默认显示库存查询页面 ========
        ContentFrame.Navigate(new StockQueryPage());
        TxtStatus.Text = "就绪";

        // ======== 底部状态栏实时时钟 ========
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (s, e) => TxtTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        timer.Start();
        TxtTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    /// <summary>
    /// 顶部导航按钮点击事件
    /// 通过 Button.Tag 区分目标页面：
    ///   "User" → 用户维护
    ///   "Item" → 物品信息
    ///   "StockIn" → 物品入库
    ///   "StockOut" → 物品出库（领用审批）
    ///   "StockQuery" → 库存查询
    /// </summary>
    private void NavButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag)
        {
            // 根据 Tag 创建对应的 Page 实例
            Page? page = tag switch
            {
                "User"       => new UserManagementPage(),
                "Item"       => new ItemManagementPage(),
                "StockIn"    => new StockInPage(),
                "StockOut"   => new StockOutPage(),
                "StockQuery" => new StockQueryPage(),
                _            => null
            };

            if (page != null)
            {
                // Frame.Navigate 切换到目标页面（类似浏览器的页面跳转）
                ContentFrame.Navigate(page);
                TxtStatus.Text = $"{btn.Content}";
            }
        }
    }

    /// <summary>退出登录 — 返回登录窗口</summary>
    private void BtnLogout_Click(object sender, RoutedEventArgs e)
    {
        var login = new LoginWindow();
        login.Show();
        this.Close();  // 关闭主窗口
    }
}

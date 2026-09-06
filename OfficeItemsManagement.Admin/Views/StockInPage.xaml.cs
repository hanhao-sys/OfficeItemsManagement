using System.Windows;
using System.Windows.Controls;
using OfficeItemsManagement.Admin.Services;

namespace OfficeItemsManagement.Admin.Views;

/// <summary>
/// 物品入库页面 — 管理员录入采购入库
/// 
/// 实验要求：
///   - 入库流水号自动生成（SI + yyyyMMdd + 序号）
///   - 总价 = 数量 × 单价，自动计算
///   - 保存后自动更新物品库存（事务保护）
///   - 物品通过弹出窗口选择（不直接输入编码）
/// </summary>
public partial class StockInPage : Page
{
    public StockInPage()
    {
        InitializeComponent();
        Loaded += (s, e) => _ = LoadSafeAsync();
    }

    /// <summary>异步安全加载</summary>
    private async Task LoadSafeAsync()
    {
        try { await Dispatcher.InvokeAsync(async () => await LoadStockInRecords()); }
        catch (Exception ex) { MessageBox.Show($"加载失败: {ex.Message}"); }
    }

    /// <summary>加载入库记录到 DataGrid（仅最新 50 条，避免百万数据卡死）</summary>
    private async Task LoadStockInRecords()
    {
        try
        {
            var records = await ServiceLocator.StockInRepo.GetRecentAsync(50);
            DgStockIn.ItemsSource = records;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载入库记录失败: {ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 自动计算总价 — 数量 × 单价的实时计算
    /// 绑定到数量和单价输入框的 TextChanged 事件
    /// </summary>
    private void CalculateTotal()
    {
        if (int.TryParse(TxtQuantity.Text, out var qty) &&
            decimal.TryParse(TxtUnitPrice.Text, out var price))
        {
            TxtTotalPrice.Text = (qty * price).ToString("F2");
        }
        else
        {
            TxtTotalPrice.Text = "0.00";
        }
    }

    /// <summary>数量文本框内容变化 → 重新计算总价</summary>
    private void TxtQuantity_TextChanged(object sender, TextChangedEventArgs e) => CalculateTotal();

    /// <summary>单价文本框内容变化 → 重新计算总价</summary>
    private void TxtUnitPrice_TextChanged(object sender, TextChangedEventArgs e) => CalculateTotal();

    /// <summary>
    /// 选择物品按钮 — 弹出物品选择窗口
    /// 窗口内显示物品 DataGrid（编码/名称/库存），选中后回填到表单
    /// </summary>
    private async void BtnSelectItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var items = await ServiceLocator.ItemRepo.GetAllAsync();
            if (!items.Any())
            {
                MessageBox.Show("暂无物品数据，请先在物品信息中添加！", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // ======== 动态创建弹出窗口 ========
            var dlg = new Window
            {
                Title = "选择物品",
                Width = 500,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };

            var grid = new Grid { Margin = new Thickness(10) };
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // 物品 DataGrid
            var dg = new System.Windows.Controls.DataGrid
            {
                AutoGenerateColumns = false,
                IsReadOnly = true,
                ItemsSource = items
            };
            dg.Columns.Add(new DataGridTextColumn
            {
                Header = "编码",
                Binding = new System.Windows.Data.Binding("ItemCode"),
                Width = 70
            });
            dg.Columns.Add(new DataGridTextColumn
            {
                Header = "名称",
                Binding = new System.Windows.Data.Binding("ItemName"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });
            dg.Columns.Add(new DataGridTextColumn
            {
                Header = "库存",
                Binding = new System.Windows.Data.Binding("Quantity"),
                Width = 60
            });
            Grid.SetRow(dg, 0);

            // 确定/取消按钮
            var btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0)
            };
            var btnOk = new Button
            {
                Content = "确定", Width = 80, Height = 30,
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0, 120, 212)),
                Foreground = System.Windows.Media.Brushes.White
            };
            var btnCancel = new Button { Content = "取消", Width = 80, Height = 30 };
            btnPanel.Children.Add(btnOk);
            btnPanel.Children.Add(btnCancel);
            Grid.SetRow(btnPanel, 1);

            // 确定 → 回填选中的物品
            btnOk.Click += (s2, e2) =>
            {
                if (dg.SelectedItem is Common.Models.Item item)
                {
                    TxtItemCode.Text = item.ItemCode;
                    TxtItemName.Text = item.ItemName;
                    TxtCurrentQty.Text = item.Quantity.ToString();
                }
                dlg.Close();
            };
            btnCancel.Click += (s2, e2) => dlg.Close();

            grid.Children.Add(dg);
            grid.Children.Add(btnPanel);
            dlg.Content = grid;
            dlg.ShowDialog();  // 模态窗口
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载物品列表失败: {ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 提交入库按钮
    /// 
    /// 执行流程：
    ///   1. 表单验证（物品、数量、单价）
    ///   2. 查找物品实体
    ///   3. 调用 ExecuteStockInAsync（事务：生成流水号 + 写入库记录 + 更新库存）
    ///   4. 刷新入库记录列表 + 清空表单
    /// </summary>
    private async void BtnSubmit_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // ======== 表单验证 ========
            var itemCode = TxtItemCode.Text.Trim();
            if (string.IsNullOrEmpty(itemCode))
            {
                MessageBox.Show("请选择物品！", "验证",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(TxtQuantity.Text, out var qty) || qty <= 0)
            {
                MessageBox.Show("购买数量必须为正整数！", "验证",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(TxtUnitPrice.Text, out var price) || price < 0)
            {
                MessageBox.Show("单价必须为非负数！", "验证",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 查找物品
            var item = await ServiceLocator.ItemRepo.GetByCodeAsync(itemCode);
            if (item == null)
            {
                MessageBox.Show("物品不存在！", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // ======== 执行入库（事务） ========
            var stockIn = await ServiceLocator.StockInRepo.ExecuteStockInAsync(
                item, qty, price);

            MessageBox.Show($"入库成功！流水号: {stockIn.StockInNo}", "成功",
                MessageBoxButton.OK, MessageBoxImage.Information);

            // 刷新页面
            await LoadStockInRecords();
            TxtItemCode.Text = "";
            TxtItemName.Text = "";
            TxtCurrentQty.Text = "";
            TxtQuantity.Text = "";
            TxtUnitPrice.Text = "";
            TxtTotalPrice.Text = "0.00";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"入库失败: {ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

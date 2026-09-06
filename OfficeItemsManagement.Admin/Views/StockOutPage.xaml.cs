using System.Windows;
using System.Windows.Controls;
using OfficeItemsManagement.Admin.Services;
using OfficeItemsManagement.Common.DTOs;

namespace OfficeItemsManagement.Admin.Views;

/// <summary>
/// 物品出库页面（领用审批） — 管理员处理普通用户的领用申请
/// 
/// 审批流程：
///   普通用户 Web 端提交 → Status=0（申请）
///   管理员在此页面 → 查看申请 → 确认(1) / 驳回(2)
///   确认时自动扣减库存（事务保护）
/// 
/// 筛选功能：
///   RadioButton 切换：全部 / 待审批 / 已确认 / 已驳回
/// </summary>
public partial class StockOutPage : Page
{
    /// <summary>全量领用记录（从数据库加载后缓存），筛选在此基础上去做</summary>
    private List<StockOutDetailDto> _allRecords = new();

    public StockOutPage()
    {
        InitializeComponent();
        Loaded += (s, e) => _ = LoadRecordsSafeAsync();
    }

    /// <summary>异步安全加载 — 异常弹窗提示，不崩溃</summary>
    private async Task LoadRecordsSafeAsync()
    {
        try
        {
            await Dispatcher.InvokeAsync(async () => await LoadRecords());
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载领用记录失败: {ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>从数据库加载所有领用记录并应用筛选</summary>
    private async Task LoadRecords()
    {
        try
        {
            _allRecords = await ServiceLocator.StockOutRepo.GetAllAsync();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载领用记录失败: {ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 按 RadioButton 选中状态筛选数据
    /// 在 _allRecords 内存列表中过滤，无需重新查数据库
    /// 
    /// ⚠️ null 检查：XAML 解析时 RadioButton Checked 可能早于 DataGrid 初始化，
    ///    此时 DgStockOut 为 null，直接跳过
    /// </summary>
    private void ApplyFilter()
    {
        if (DgStockOut == null) return;  // 还未初始化完成

        var filtered = _allRecords.AsEnumerable();

        if (RbPending.IsChecked == true)
            filtered = filtered.Where(r => r.Status == 0);   // 待审批
        else if (RbApproved.IsChecked == true)
            filtered = filtered.Where(r => r.Status == 1);   // 已确认
        else if (RbRejected.IsChecked == true)
            filtered = filtered.Where(r => r.Status == 2);   // 已驳回
        // 否则"全部"：不过滤

        DgStockOut.ItemsSource = filtered.ToList();
    }

    /// <summary>筛选按钮切换事件</summary>
    private void FilterChanged(object sender, RoutedEventArgs e) => ApplyFilter();

    /// <summary>
    /// 确认领用按钮 — 通过审批
    /// 
    /// 执行前检查：
    ///   1. 是否选中记录
    ///   2. 状态是否为"申请"（防止重复审批）
    ///   3. 二次确认弹窗
    /// 
    /// 执行后：
    ///   调用 ApproveAsync → 事务更新状态 + 扣减库存
    /// </summary>
    private async void BtnApprove_Click(object sender, RoutedEventArgs e)
    {
        // 检查是否选中
        if (DgStockOut.SelectedItem is not StockOutDetailDto record)
        {
            MessageBox.Show("请先选择一条领用记录！", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 状态校验
        if (record.Status != 0)
        {
            MessageBox.Show("只能确认「申请」状态的记录！", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 二次确认
        var result = MessageBox.Show(
            $"确认领用: {record.ItemName} × {record.Quantity}，领用人: {record.ApplicantId}？",
            "确认领用", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            await ServiceLocator.StockOutRepo.ApproveAsync(
                record.StockOutNo, "管理员确认");
            MessageBox.Show("领用已确认，库存已扣减！", "成功",
                MessageBoxButton.OK, MessageBoxImage.Information);
            await LoadRecords();  // 刷新列表
        }
        catch (Exception ex)
        {
            MessageBox.Show($"确认失败: {ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 驳回申请按钮 — 拒绝领用
    /// 不扣减库存，仅更新状态为"驳回"
    /// </summary>
    private async void BtnReject_Click(object sender, RoutedEventArgs e)
    {
        if (DgStockOut.SelectedItem is not StockOutDetailDto record)
        {
            MessageBox.Show("请先选择一条领用记录！", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (record.Status != 0)
        {
            MessageBox.Show("只能驳回「申请」状态的记录！", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show(
            $"驳回申请: {record.ItemName} × {record.Quantity}，领用人: {record.ApplicantId}？",
            "驳回申请", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            await ServiceLocator.StockOutRepo.RejectAsync(
                record.StockOutNo, "管理员驳回");
            MessageBox.Show("申请已驳回！", "成功",
                MessageBoxButton.OK, MessageBoxImage.Information);
            await LoadRecords();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"驳回失败: {ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>刷新按钮</summary>
    private async void BtnRefresh_Click(object sender, RoutedEventArgs e) => await LoadRecords();
}

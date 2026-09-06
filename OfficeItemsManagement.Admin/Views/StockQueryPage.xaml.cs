using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using OfficeItemsManagement.Admin.Services;
using OfficeItemsManagement.Common.DTOs;

namespace OfficeItemsManagement.Admin.Views;

/// <summary>
/// 库存查询页面 — 三个 Tab 页签
/// 
/// Tab 1: 库存总览
///   - 显示所有物品的库存信息（编码/名称/类别/产地/规格/型号/数量）
///   - 支持按类别筛选（ComboBox）
///   - 零库存物品标红
/// 
/// Tab 2: 入库明细
///   - 入库记录列表（JOIN Items 获取物品名称）
///   - 支持按物品编码筛选
///   - 分页显示（每页 20 条）
/// 
/// Tab 3: 领用明细
///   - 领用记录列表
///   - 支持按物品编码筛选
///   - 分页显示
/// </summary>
public partial class StockQueryPage : Page
{
    /// <summary>入库明细当前页码</summary>
    private int _inPage = 1;

    /// <summary>领用明细当前页码</summary>
    private int _outPage = 1;

    /// <summary>每页显示条数</summary>
    private const int PageSize = 20;

    public StockQueryPage()
    {
        InitializeComponent();
        Loaded += (s, e) => _ = LoadSafeAsync();
    }

    private async Task LoadSafeAsync()
    {
        try { await Dispatcher.InvokeAsync(async () => await Initialize()); }
        catch (Exception ex) { MessageBox.Show($"加载失败: {ex.Message}"); }
    }

    /// <summary>
    /// 初始化 — 加载类别下拉 + 三个 Tab 的初始数据
    /// </summary>
    private async Task Initialize()
    {
        try
        {
            // 加载类别筛选下拉框
            var categories = await ServiceLocator.CategoryRepo.GetAllAsync();
            CmbCategoryFilter.ItemsSource = categories;

            // 同时加载三个 Tab 的数据
            await LoadStock();
            await LoadInDetail();
            await LoadOutDetail();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载数据失败: {ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ==================== Tab 1: 库存总览 ====================

    /// <summary>
    /// 加载库存总览 — JOIN Items + Categories
    /// </summary>
    /// <param name="catId">类别 ID（null = 全部）</param>
    private async Task LoadStock(string? catId = null)
    {
        try
        {
            var items = await ServiceLocator.ItemRepo.GetStockQueryAsync(catId);
            DgStock.ItemsSource = items;
            // DataGrid 的 DataTrigger 会自动将 Quantity=0 的行标红
        }
        catch (Exception ex)
        {
            MessageBox.Show($"查询库存失败: {ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>按类别查询</summary>
    private async void BtnStockQuery_Click(object sender, RoutedEventArgs e)
    {
        var catId = CmbCategoryFilter.SelectedValue?.ToString();
        await LoadStock(catId);
    }

    /// <summary>显示全部</summary>
    private async void BtnStockQueryAll_Click(object sender, RoutedEventArgs e)
    {
        CmbCategoryFilter.SelectedIndex = -1;
        await LoadStock();
    }

    private async void BtnStockRefresh_Click(object sender, RoutedEventArgs e) => await LoadStock();

    // ==================== Tab 2: 入库明细 ====================

    /// <summary>
    /// 加载入库明细 — 分页查询
    /// 直接使用 DbContext 的 LINQ 查询，JOIN Items 表获取物品名称
    /// </summary>
    /// <param name="itemCode">按物品编码筛选（空 = 全部）</param>
    private async Task LoadInDetail(string? itemCode = null)
    {
        try
        {
            var db = ServiceLocator.DbContext;

            // JOIN 查询：StockIn + Items
            var query = from si in db.StockIns
                        join i in db.Items on si.ItemCode equals i.ItemCode
                        select new StockInDetailDto
                        {
                            StockInNo = si.StockInNo,
                            ItemCode = si.ItemCode,
                            ItemName = i.ItemName,
                            PurchaseDate = si.PurchaseDate,
                            Quantity = si.Quantity,
                            UnitPrice = si.UnitPrice,
                            TotalPrice = si.TotalPrice
                        };

            // 可选：按物品编码筛选
            if (!string.IsNullOrEmpty(itemCode))
                query = query.Where(q => q.ItemCode == itemCode);

            // 分页：按日期倒序，跳过前 N 条，取 PageSize 条
            var data = await query
                .OrderByDescending(q => q.PurchaseDate)
                .Skip((_inPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            DgStockInDetail.ItemsSource = data;
            TxtInPage.Text = $"第 {_inPage} 页";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"查询入库明细失败: {ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnInDetailQuery_Click(object sender, RoutedEventArgs e)
    {
        _inPage = 1;  // 查询时重置到第 1 页
        await LoadInDetail(TxtInItemCode.Text.Trim());
    }

    private async void BtnInDetailRefresh_Click(object sender, RoutedEventArgs e)
    {
        _inPage = 1;
        TxtInItemCode.Text = "";
        await LoadInDetail();
    }

    /// <summary>上一页 — 页码 > 1 时才有效</summary>
    private async void BtnInPrev_Click(object sender, RoutedEventArgs e)
    {
        if (_inPage > 1) { _inPage--; await LoadInDetail(TxtInItemCode.Text.Trim()); }
    }

    /// <summary>下一页</summary>
    private async void BtnInNext_Click(object sender, RoutedEventArgs e)
    {
        _inPage++; await LoadInDetail(TxtInItemCode.Text.Trim());
    }

    // ==================== Tab 3: 领用明细 ====================

    /// <summary>加载领用明细 — 分页查询（委托给 StockOutRepo）</summary>
    private async Task LoadOutDetail(string? itemCode = null)
    {
        try
        {
            var data = await ServiceLocator.StockOutRepo.GetDetailPageAsync(
                string.IsNullOrEmpty(itemCode) ? null : itemCode, _outPage, PageSize);
            DgStockOutDetail.ItemsSource = data;
            TxtOutPage.Text = $"第 {_outPage} 页";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"查询领用明细失败: {ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnOutDetailQuery_Click(object sender, RoutedEventArgs e)
    {
        _outPage = 1;
        await LoadOutDetail(TxtOutItemCode.Text.Trim());
    }

    private async void BtnOutDetailRefresh_Click(object sender, RoutedEventArgs e)
    {
        _outPage = 1;
        TxtOutItemCode.Text = "";
        await LoadOutDetail();
    }

    private async void BtnOutPrev_Click(object sender, RoutedEventArgs e)
    {
        if (_outPage > 1) { _outPage--; await LoadOutDetail(TxtOutItemCode.Text.Trim()); }
    }

    private async void BtnOutNext_Click(object sender, RoutedEventArgs e)
    {
        _outPage++; await LoadOutDetail(TxtOutItemCode.Text.Trim());
    }
}

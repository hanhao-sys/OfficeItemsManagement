using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using OfficeItemsManagement.Admin.Services;
using OfficeItemsManagement.Common.Models;

namespace OfficeItemsManagement.Admin.Views;

/// <summary>
/// 物品信息维护页面 — 管理员对物品的增删改操作
/// 图片存储在数据库 ImageData 字段（LONGBLOB），不再复制到文件夹
/// </summary>
public partial class ItemManagementPage : Page
{
    /// <summary>当前选择的图片文件路径（仅用于选择阶段）</summary>
    private string _selectedImagePath = "";

    public ItemManagementPage()
    {
        InitializeComponent();
        Loaded += (s, e) => _ = LoadSafeAsync();
    }

    private async Task LoadSafeAsync()
    {
        try { await Dispatcher.InvokeAsync(async () => await RefreshList()); }
        catch (Exception ex) { MessageBox.Show($"加载失败: {ex.Message}"); }
    }

    private async Task RefreshList()
    {
        try
        {
            var items = await ServiceLocator.ItemRepo.GetAllAsync();
            DgItems.ItemsSource = items;
            ClearForm();

            var origins = await ServiceLocator.OriginRepo.GetAllAsync();
            CmbOrigin.ItemsSource = origins;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载数据失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ClearForm()
    {
        TxtItemCode.Text = "";
        TxtItemCode.IsEnabled = true;
        TxtItemName.Text = "";
        RbOther.IsChecked = true;
        if (CmbOrigin.Items.Count > 0) CmbOrigin.SelectedIndex = 0;
        TxtSpec.Text = "";
        TxtModel.Text = "";
        _selectedImagePath = "";
        ImgPreview.Source = null;
        TxtImagePath.Text = "";
        EditPanel.IsEnabled = false;
    }

    /// <summary>
    /// 选中物品 → 回填表单，图片优先从 DB（ImageData）加载，fallback 到文件
    /// </summary>
    private void DgItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DgItems.SelectedItem is Item item)
        {
            TxtItemCode.Text = item.ItemCode;
            TxtItemCode.IsEnabled = false;
            TxtItemName.Text = item.ItemName;
            SetCategoryRadio(item.Category);
            CmbOrigin.Text = item.Origin;
            TxtSpec.Text = item.Specification;
            TxtModel.Text = item.Model;
            _selectedImagePath = item.ImagePath;
            TxtImagePath.Text = string.IsNullOrEmpty(item.ImagePath) ? "（存储在数据库）" : item.ImagePath;

            // ======== 加载图片：优先数据库 ImageData，fallback 文件 ========
            if (item.ImageData != null && item.ImageData.Length > 0)
            {
                // 从数据库 byte[] 加载
                try
                {
                    var bmp = new System.Windows.Media.Imaging.BitmapImage();
                    using var ms = new MemoryStream(item.ImageData);
                    bmp.BeginInit();
                    bmp.StreamSource = ms;
                    bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    ImgPreview.Source = bmp;
                }
                catch { ImgPreview.Source = null; }
            }
            else if (!string.IsNullOrEmpty(item.ImagePath) && File.Exists(item.ImagePath))
            {
                // 兼容旧数据：从文件加载
                try
                {
                    var bmp = new System.Windows.Media.Imaging.BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(item.ImagePath, UriKind.Absolute);
                    bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    ImgPreview.Source = bmp;
                }
                catch { ImgPreview.Source = null; }
            }
            else
            {
                ImgPreview.Source = null;
            }

            EditPanel.IsEnabled = true;
        }
    }

    private void SetCategoryRadio(int cat)
    {
        RbPaper.IsChecked = cat == 1;
        RbStationery.IsChecked = cat == 2;
        RbCutter.IsChecked = cat == 3;
        RbDoc.IsChecked = cat == 4;
        RbGift.IsChecked = cat == 5;
        RbOther.IsChecked = cat == 6;
    }

    private int GetSelectedCategory()
    {
        if (RbPaper.IsChecked == true) return 1;
        if (RbStationery.IsChecked == true) return 2;
        if (RbCutter.IsChecked == true) return 3;
        if (RbDoc.IsChecked == true) return 4;
        if (RbGift.IsChecked == true) return 5;
        return 6;
    }

    private async void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        ClearForm();
        TxtItemCode.IsEnabled = true;
        EditPanel.IsEnabled = true;
        TxtItemCode.Focus();
    }

    /// <summary>
    /// 保存按钮 — 图片以 byte[] 存入数据库 ImageData 列
    /// </summary>
    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var code = TxtItemCode.Text.Trim();
            var name = TxtItemName.Text.Trim();

            if (string.IsNullOrEmpty(code)) { ShowWarning("物品编码不能为空！"); return; }
            if (string.IsNullOrEmpty(name)) { ShowWarning("物品名称不能为空！"); return; }

            // ======== 图片 → byte[] 存入数据库 ========
            byte[]? imageData = null;
            var imagePath = "";
            if (!string.IsNullOrEmpty(_selectedImagePath) && File.Exists(_selectedImagePath))
            {
                imageData = File.ReadAllBytes(_selectedImagePath);
                imagePath = _selectedImagePath; // 保留原路径作记录
            }

            // 判断新增/修改
            var existing = await ServiceLocator.ItemRepo.GetByCodeAsync(code);
            if (existing != null && TxtItemCode.IsEnabled)
            {
                ShowWarning($"物品编码 '{code}' 已存在！");
                return;
            }

            var item = new Item
            {
                ItemCode = code,
                ItemName = name,
                Category = GetSelectedCategory(),
                Origin = CmbOrigin.Text ?? "",
                Specification = TxtSpec.Text.Trim(),
                Model = TxtModel.Text.Trim(),
                ImagePath = imagePath,
                ImageData = imageData,           // ← 图片二进制存数据库
                Quantity = existing?.Quantity ?? 0
            };

            if (existing != null)
                await ServiceLocator.ItemRepo.UpdateAsync(item);
            else
                await ServiceLocator.ItemRepo.AddAsync(item);

            await RefreshList();
            MessageBox.Show("保存成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (DgItems.SelectedItem is not Item item)
        { ShowWarning("请先选择要删除的物品！"); return; }

        var result = MessageBox.Show($"确定删除物品 '{item.ItemCode} - {item.ItemName}' 吗？",
            "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                await ServiceLocator.ItemRepo.DeleteAsync(item.ItemCode);
                await RefreshList();
                MessageBox.Show("删除成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e) => await RefreshList();

    private async void BtnSearch_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var keyword = TxtSearch.Text.Trim();
            if (string.IsNullOrEmpty(keyword))
            {
                await RefreshList();
                return;
            }
            var items = await ServiceLocator.ItemRepo.SearchAsync(keyword);
            DgItems.ItemsSource = items;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"搜索失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnSelectImage_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "选择物品图片",
            Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp;*.gif|所有文件|*.*"
        };

        if (dlg.ShowDialog() == true)
        {
            _selectedImagePath = dlg.FileName;
            TxtImagePath.Text = dlg.FileName;

            try
            {
                var bmp = new System.Windows.Media.Imaging.BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(dlg.FileName, UriKind.Absolute);
                bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bmp.EndInit();
                ImgPreview.Source = bmp;
            }
            catch { ImgPreview.Source = null; }
        }
    }

    private void ShowWarning(string msg) =>
        MessageBox.Show(msg, "验证", MessageBoxButton.OK, MessageBoxImage.Warning);
}

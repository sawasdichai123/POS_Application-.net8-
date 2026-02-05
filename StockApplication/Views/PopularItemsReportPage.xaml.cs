using StockApplication.Models;
using StockApplication.Repositories;
using System.Collections.ObjectModel;
using System.Text;
using System.IO;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using iTextSharp.text;
using iTextSharp.text.pdf;
using PdfElement = iTextSharp.text.Element; // Added alias to resolve the ambiguity
using PdfFont = iTextSharp.text.Font; // Added alias to resolve Font ambiguity

namespace StockApplication.Views.Reports;

public partial class PopularItemsReportPage : ContentPage
{
    private readonly Repository<OrderDetail> _orderDetailRepository;
    private readonly Repository<MenuItem> _menuItemRepository;
    private readonly Repository<Order> _orderRepository;
    
    private ObservableCollection<PopularItemViewModel> _popularItems = new();
    private ObservableCollection<CategorySalesViewModel> _categorySales = new();
    private int _topItemsCount = 10;
    
    public PopularItemsReportPage(
        Repository<OrderDetail> orderDetailRepository,
        Repository<MenuItem> menuItemRepository,
        Repository<Order> orderRepository)
    {
        InitializeComponent();
        
        _orderDetailRepository = orderDetailRepository;
        _menuItemRepository = menuItemRepository;
        _orderRepository = orderRepository;
        
        // Set up collection views
        PopularItemsCollection.ItemsSource = _popularItems;
        CategorySalesCollection.ItemsSource = _categorySales;
        
        // Set default date range (last 30 days)
        StartDatePicker.Date = DateTime.Now.AddDays(-30);
        EndDatePicker.Date = DateTime.Now;
        
        // Set up top items slider
        TopItemsSlider.Value = _topItemsCount;
        TopItemsValueLabel.Text = _topItemsCount.ToString();
        TopItemsSlider.ValueChanged += (s, e) => {
            _topItemsCount = (int)Math.Round(e.NewValue);
            TopItemsValueLabel.Text = _topItemsCount.ToString();
        };
        
        // Set up event handlers
        ApplyButton.Clicked += OnApplyClicked;
        ExportCsvButton.Clicked += OnExportCsvClicked;
        ExportPdfButton.Clicked += OnExportPdfClicked;
        
        // Load data
        LoadPopularItemsData();
    }
    
    private async void LoadPopularItemsData()
    {
        try
        {
            // Show loading indicator
            IsBusy = true;
            
            // Get all order details
            var allOrderDetails = await _orderDetailRepository.GetAllAsync();
            
            // Get all orders for date filtering
            var allOrders = await _orderRepository.GetAllAsync();
            var orderDict = allOrders.ToDictionary(o => o.OrderID);
            
            // Filter by date range
            var startDate = StartDatePicker.Date;
            var endDate = EndDatePicker.Date.AddDays(1).AddSeconds(-1); // End of the selected day
            
            var filteredOrderDetails = allOrderDetails.Where(od => 
                orderDict.ContainsKey(od.OrderID) && 
                orderDict[od.OrderID].OrderDate >= startDate && 
                orderDict[od.OrderID].OrderDate <= endDate).ToList();
            
            // Get menu items for lookup
            var menuItems = await _menuItemRepository.GetAllAsync();
            var menuItemDict = menuItems.ToDictionary(m => m.MenuID);
            
            // Group by menu item and calculate totals
            var popularItemsData = filteredOrderDetails
                .GroupBy(od => od.MenuID)
                .Select(g => {
                    var menuItem = menuItemDict.ContainsKey(g.Key) ? menuItemDict[g.Key] : null;
                    return new 
                    {
                        MenuID = g.Key,
                        MenuName = menuItem?.Menu_Name ?? $"Item #{g.Key}",
                        Category = menuItem?.Category ?? "Unknown",
                        Quantity = g.Sum(od => od.Quantity),
                        Revenue = g.Sum(od => od.Subtotal)
                    };
                })
                .OrderByDescending(x => x.Quantity)
                .Take(_topItemsCount)
                .ToList();
            
            // Clear and populate popular items collection
            _popularItems.Clear();
            int rank = 1;
            foreach (var item in popularItemsData)
            {
                _popularItems.Add(new PopularItemViewModel
                {
                    Rank = rank++,
                    MenuID = item.MenuID,
                    Menu_Name = item.MenuName,
                    Category = item.Category,
                    Quantity = item.Quantity,
                    Revenue = item.Revenue
                });
            }
            
            // Group by category and calculate totals
            var categorySalesData = filteredOrderDetails
                .Where(od => menuItemDict.ContainsKey(od.MenuID))
                .GroupBy(od => menuItemDict[od.MenuID].Category ?? "Unknown")
                .Select(g => new CategorySalesViewModel
                {
                    Category = g.Key,
                    Quantity = g.Sum(od => od.Quantity),
                    Revenue = g.Sum(od => od.Subtotal)
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();
            
            // Clear and populate category sales collection
            _categorySales.Clear();
            foreach (var category in categorySalesData)
            {
                _categorySales.Add(category);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load popular items data: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private void OnApplyClicked(object sender, EventArgs e)
    {
        LoadPopularItemsData();
    }
    
    private async void OnExportCsvClicked(object sender, EventArgs e)
    {
        try
        {
            // Create CSV content
            var csvContent = new StringBuilder();
            csvContent.AppendLine("Rank,Menu ID,Name,Category,Quantity,Revenue");
            
            foreach (var item in _popularItems)
            {
                csvContent.AppendLine(
                    $"{item.Rank}," +
                    $"{item.MenuID}," +
                    $"{EscapeCsvField(item.Menu_Name)}," +
                    $"{EscapeCsvField(item.Category)}," +
                    $"{item.Quantity}," +
                    $"{item.Revenue:F2}");
            }
            
            // Add category sales
            csvContent.AppendLine("\nCategory Sales");
            csvContent.AppendLine("Category,Quantity,Revenue");
            
            foreach (var category in _categorySales)
            {
                csvContent.AppendLine(
                    $"{EscapeCsvField(category.Category)}," +
                    $"{category.Quantity}," +
                    $"{category.Revenue:F2}");
            }
            
            // Generate file name with timestamp
            string fileName = $"Popular_Items_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            
            // Write the file
            File.WriteAllText(filePath, csvContent.ToString());
            
            // Ask user for sharing or saving
            string action = await DisplayActionSheet(
                "Export Options", 
                "Cancel", 
                null, 
                "Share File", 
                "Save to Device");
            
            switch (action)
            {
                case "Share File":
                    // Use built-in sharing
                    await Share.RequestAsync(new ShareFileRequest
                    {
                        Title = "Export Popular Items Report",
                        File = new ShareFile(filePath)
                    });
                    break;
                    
                case "Save to Device":
                    // Use platform-specific code to save file
                    await SaveFileWithPlatformSpecificCode(filePath, fileName);
                    break;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to export CSV: {ex.Message}", "OK");
        }
    }
    
    private async void OnExportPdfClicked(object sender, EventArgs e)
    {
        try
        {
            // Create PDF document
            var document = new Document(PageSize.A4, 36, 36, 54, 36);
            string fileName = $"Popular_Items_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            
            // Create PDF writer - Using proper disposal with using statement
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                var writer = PdfWriter.GetInstance(document, fileStream);
                document.Open();
                
                // Add title
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, BaseColor.Blue);
                var title = new Paragraph("Popular Items Report", titleFont);
                title.Alignment = PdfElement.ALIGN_CENTER;
                document.Add(title);
                document.Add(new Paragraph(" ")); // Add spacing
                
                // Add date range
                var dateRangeFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);
                var dateRange = new Paragraph($"Date Range: {StartDatePicker.Date:MM/dd/yyyy} to {EndDatePicker.Date:MM/dd/yyyy}", dateRangeFont);
                document.Add(dateRange);
                document.Add(new Paragraph(" ")); // Add spacing
                
                // Add top items table header
                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, BaseColor.Blue);
                var topItemsHeader = new Paragraph($"Top {_topItemsCount} Selling Items", headerFont);
                document.Add(topItemsHeader);
                document.Add(new Paragraph(" ")); // Add spacing
                
                // Create table for popular items
                var table = new PdfPTable(6);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 1f, 1.5f, 4f, 3f, 2f, 2.5f });
                
                // Add table headers
                var cellFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.White);
                
                string[] headers = { "Rank", "ID", "Name", "Category", "Quantity", "Revenue" };
                foreach (var header in headers)
                {
                    var headerCell = new PdfPCell(new Phrase(header, cellFont));
                    headerCell.BackgroundColor = new BaseColor(33, 150, 243); // #2196F3
                    headerCell.HorizontalAlignment = PdfElement.ALIGN_CENTER;
                    headerCell.VerticalAlignment = PdfElement.ALIGN_MIDDLE;
                    headerCell.Padding = 8;
                    table.AddCell(headerCell);
                }
                
                // Add data rows
                var regularFont = FontFactory.GetFont(FontFactory.HELVETICA, 11);
                var alternateRowColor = new BaseColor(240, 240, 240);
                bool isAlternateRow = false;
                
                foreach (var item in _popularItems)
                {
                    // Array of values to display
                    var values = new string[] 
                    { 
                        item.Rank.ToString(), 
                        item.MenuID.ToString(), 
                        item.Menu_Name, 
                        item.Category, 
                        item.Quantity.ToString(), 
                        $"฿{item.Revenue:F2}" 
                    };
                    
                    for (int i = 0; i < values.Length; i++)
                    {
                        var cell = new PdfPCell(new Phrase(values[i], regularFont));
                        
                        if (isAlternateRow)
                            cell.BackgroundColor = alternateRowColor;
                        
                        // Alignment
                        if (i == 0) // Rank
                            cell.HorizontalAlignment = PdfElement.ALIGN_CENTER;
                        else if (i >= 4) // Quantity, Revenue
                            cell.HorizontalAlignment = PdfElement.ALIGN_RIGHT;
                        else
                            cell.HorizontalAlignment = PdfElement.ALIGN_LEFT;
                        
                        cell.VerticalAlignment = PdfElement.ALIGN_MIDDLE;
                        cell.Padding = 6;
                        table.AddCell(cell);
                    }
                    
                    isAlternateRow = !isAlternateRow;
                }
                
                document.Add(table);
                document.Add(new Paragraph(" ")); // Add spacing
                
                // Add category sales header
                var categoryHeader = new Paragraph("Sales by Category", headerFont);
                document.Add(categoryHeader);
                document.Add(new Paragraph(" ")); // Add spacing
                
                // Create table for category sales
                var categoryTable = new PdfPTable(3);
                categoryTable.WidthPercentage = 100;
                categoryTable.SetWidths(new float[] { 5f, 2f, 3f });
                
                // Add table headers
                string[] categoryHeaders = { "Category", "Quantity", "Revenue" };
                foreach (var header in categoryHeaders)
                {
                    var headerCell = new PdfPCell(new Phrase(header, cellFont));
                    headerCell.BackgroundColor = new BaseColor(33, 150, 243); // #2196F3
                    headerCell.HorizontalAlignment = PdfElement.ALIGN_CENTER;
                    headerCell.VerticalAlignment = PdfElement.ALIGN_MIDDLE;
                    headerCell.Padding = 8;
                    categoryTable.AddCell(headerCell);
                }
                
                // Add category data rows
                isAlternateRow = false;
                
                foreach (var category in _categorySales)
                {
                    // Array of values to display
                    var values = new string[] 
                    { 
                        category.Category, 
                        category.Quantity.ToString(), 
                        $"฿{category.Revenue:F2}" 
                    };
                    
                    for (int i = 0; i < values.Length; i++)
                    {
                        var cell = new PdfPCell(new Phrase(values[i], regularFont));
                        
                        if (isAlternateRow)
                            cell.BackgroundColor = alternateRowColor;
                        
                        // Alignment
                        if (i == 0) // Category
                            cell.HorizontalAlignment = PdfElement.ALIGN_LEFT;
                        else // Quantity, Revenue
                            cell.HorizontalAlignment = PdfElement.ALIGN_RIGHT;
                        
                        cell.VerticalAlignment = PdfElement.ALIGN_MIDDLE;
                        cell.Padding = 6;
                        categoryTable.AddCell(cell);
                    }
                    
                    isAlternateRow = !isAlternateRow;
                }
                
                document.Add(categoryTable);
                
                // Add footer with report generation information
                document.Add(new Paragraph(" ")); // Add spacing
                var footerFont = FontFactory.GetFont(FontFactory.HELVETICA, 8, PdfFont.ITALIC, BaseColor.Gray);
                var footer = new Paragraph($"Generated on {DateTime.Now:MM/dd/yyyy HH:mm:ss}", footerFont);
                footer.Alignment = PdfElement.ALIGN_RIGHT;
                document.Add(footer);
                
                // Close document
                document.Close();
            }
            
            // Ask user for sharing or saving
            string action = await DisplayActionSheet(
                "Export Options", 
                "Cancel", 
                null, 
                "Share File", 
                "Save to Device");
            
            switch (action)
            {
                case "Share File":
                    // Use built-in sharing
                    await Share.RequestAsync(new ShareFileRequest
                    {
                        Title = "Export Popular Items Report as PDF",
                        File = new ShareFile(filePath)
                    });
                    break;
                    
                case "Save to Device":
                    // Use platform-specific code to save file
                    await SaveFileWithPlatformSpecificCode(filePath, fileName);
                    break;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to export PDF: {ex.Message}", "OK");
        }
    }
    
    // Helper method for proper CSV escaping
    private string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field)) return "";
        
        bool requiresQuoting = field.Contains(",") || field.Contains("\"") || field.Contains("\r") || field.Contains("\n");
        
        if (requiresQuoting)
        {
            // Replace quotes with double quotes and wrap in quotes
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        
        return field;
    }
    
    // Platform-specific save implementation
    private async Task SaveFileWithPlatformSpecificCode(string sourcePath, string fileName)
    {
#if ANDROID
        try 
        {
            // For Android 10+ (API 29) and above, including your Android 16 (API 36)
            // Simplified implementation without additional permission requests
            var values = new Android.Content.ContentValues();
            values.Put(Android.Provider.MediaStore.IMediaColumns.DisplayName, fileName);
            values.Put(Android.Provider.MediaStore.IMediaColumns.MimeType, GetMimeType(fileName));
            values.Put(Android.Provider.MediaStore.IMediaColumns.RelativePath, Android.OS.Environment.DirectoryDownloads);
            
            var resolver = Android.App.Application.Context.ContentResolver;
            var uri = resolver.Insert(Android.Provider.MediaStore.Downloads.ExternalContentUri, values);
            
            if (uri != null)
            {
                using (var outputStream = resolver.OpenOutputStream(uri))
                using (var fileStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read))
                {
                    await fileStream.CopyToAsync(outputStream);
                }
                
                await DisplayAlert("Success", $"File saved to Downloads: {fileName}", "OK");
            }
            else
            {
                // Fallback if insertion fails
                var downloadsPath = Android.App.Application.Context.GetExternalFilesDir(Android.OS.Environment.DirectoryDownloads).AbsolutePath;
                string destinationPath = Path.Combine(downloadsPath, fileName);
                File.Copy(sourcePath, destinationPath, true);
                await DisplayAlert("Success", $"File saved to app's Downloads folder: {fileName}", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to save file: {ex.Message}", "OK");
        }
#elif IOS
        try
        {
            // iOS specific code
            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var destinationPath = Path.Combine(documents, fileName);
            File.Copy(sourcePath, destinationPath, true);
            await DisplayAlert("Success", $"File saved to Documents folder: {fileName}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to save file: {ex.Message}", "OK");
        }
#else
        await DisplayAlert("Not Supported", "Saving files is not supported on this platform", "OK");
#endif
    }
    
    // Helper method to determine MIME type based on file extension
    private string GetMimeType(string fileName)
    {
        string extension = Path.GetExtension(fileName).ToLower();
        switch (extension)
        {
            case ".pdf":
                return "application/pdf";
            case ".csv":
                return "text/csv";
            case ".txt":
                return "text/plain";
            case ".jpg":
            case ".jpeg":
                return "image/jpeg";
            case ".png":
                return "image/png";
            default:
                return "application/octet-stream";
        }
    }
}

// View model for popular items
public class PopularItemViewModel
{
    public int Rank { get; set; }
    public int MenuID { get; set; }
    public string Menu_Name { get; set; }
    public string Category { get; set; }
    public int Quantity { get; set; }
    public double Revenue { get; set; }
}

// View model for category sales
public class CategorySalesViewModel
{
    public string Category { get; set; }
    public int Quantity { get; set; }
    public double Revenue { get; set; }
}
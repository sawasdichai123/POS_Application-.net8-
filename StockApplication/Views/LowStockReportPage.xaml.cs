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

public partial class LowStockReportPage : ContentPage
{
    private readonly StockRepository _stockRepository;
    private readonly Repository<Supplier> _supplierRepository;
    private ObservableCollection<StockItemViewModel> _lowStockItems = new();
    private double _threshold = 10;
    
    public LowStockReportPage(StockRepository stockRepository, Repository<Supplier> supplierRepository)
    {
        InitializeComponent();
        
        _stockRepository = stockRepository;
        _supplierRepository = supplierRepository;
        
        // Set up collection view
        LowStockItemsCollection.ItemsSource = _lowStockItems;
        
        // Set up threshold slider
        ThresholdSlider.Value = _threshold;
        ThresholdValueLabel.Text = _threshold.ToString("F0");
        ThresholdSlider.ValueChanged += (s, e) => {
            _threshold = Math.Round(e.NewValue);
            ThresholdValueLabel.Text = _threshold.ToString("F0");
        };
        
        // Set up event handlers
        ApplyButton.Clicked += OnApplyClicked;
        ExportCsvButton.Clicked += OnExportCsvClicked;
        ExportPdfButton.Clicked += OnExportPdfClicked; // Changed from OrderItemsButton to ExportPdfButton
        
        // Load data
        LoadLowStockData();
    }
    
    private async void LoadLowStockData()
    {
        try
        {
            // Show loading indicator
            IsBusy = true;
            
            // Get low stock items
            var lowStocks = await _stockRepository.GetLowStockAsync(_threshold);
            
            // Get all stocks for percentage calculation
            var allStocks = await _stockRepository.GetAllAsync();
            
            // Get all suppliers for lookup
            var suppliers = await _supplierRepository.GetAllAsync();
            var supplierDict = suppliers.ToDictionary(s => s.SupplierID);
            
            // Calculate statistics
            int lowStockCount = lowStocks.Count;
            double percentage = allStocks.Count > 0 ? (double)lowStockCount / allStocks.Count * 100 : 0;
            
            // Update summary labels
            MainThread.BeginInvokeOnMainThread(() => {
                LowStockCountLabel.Text = lowStockCount.ToString();
                PercentageLabel.Text = $"{percentage:F1}%";
            });
            
            // Clear and populate items collection
            _lowStockItems.Clear();
            foreach (var stock in lowStocks)
            {
                string supplierName = "Unknown";
                if (stock.SupplierID.HasValue && supplierDict.TryGetValue(stock.SupplierID.Value, out var supplier))
                {
                    supplierName = supplier.Sup_name;
                }
                
                _lowStockItems.Add(new StockItemViewModel
                {
                    StockID = stock.StockID,
                    Stock_Name = stock.Stock_Name,
                    Quantity = stock.Quantity,
                    Unit = stock.Unit ?? "",
                    SupplierName = supplierName,
                    UpdatedAt = stock.UpdatedAt
                });
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load low stock data: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private void OnApplyClicked(object sender, EventArgs e)
    {
        LoadLowStockData();
    }
    
    private async void OnExportCsvClicked(object sender, EventArgs e)
    {
        try
        {
            // Create CSV content
            var csvContent = new StringBuilder();
            csvContent.AppendLine("Stock ID,Name,Quantity,Unit,Supplier,Last Updated");
            
            foreach (var item in _lowStockItems)
            {
                csvContent.AppendLine(
                    $"{item.StockID}," +
                    $"{EscapeCsvField(item.Stock_Name)}," +
                    $"{item.Quantity}," +
                    $"{EscapeCsvField(item.Unit)}," +
                    $"{EscapeCsvField(item.SupplierName)}," +
                    $"{item.UpdatedAt:yyyy-MM-dd}");
            }
            
            // Generate file name with timestamp
            string fileName = $"Low_Stock_Report_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
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
                        Title = "Export Low Stock Report",
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
            string fileName = $"Low_Stock_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            
            // Create PDF writer - Using proper disposal with using statement
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                var writer = PdfWriter.GetInstance(document, fileStream);
                document.Open();
                
                // Add title
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, new BaseColor(211, 47, 47)); // Red color
                var title = new Paragraph("Low Stock Report", titleFont);
                title.Alignment = PdfElement.ALIGN_CENTER; // Using alias instead of Element
                document.Add(title);
                document.Add(new Paragraph(" ")); // Add spacing
                
                // Add threshold information
                var infoFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);
                var thresholdInfo = new Paragraph(
                    $"Items with quantity below {_threshold:F0} (as of {DateTime.Now:MM/dd/yyyy})", infoFont);
                document.Add(thresholdInfo);
                document.Add(new Paragraph(" ")); // Add spacing
                
                // Add summary statistics
                var summaryFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, new BaseColor(211, 47, 47)); // Red color
                var summaryHeader = new Paragraph("Summary Statistics", summaryFont);
                document.Add(summaryHeader);
                document.Add(new Paragraph(" ")); // Add spacing
                
                // Create summary table
                var summaryTable = new PdfPTable(2);
                summaryTable.WidthPercentage = 100;
                summaryTable.SetWidths(new float[] { 1f, 1f });
                
                // Summary table headers
                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.White);
                
                string[] summaryHeaders = { "Items Below Threshold", "Percentage of Inventory" };
                foreach (var header in summaryHeaders)
                {
                    var headerCell = new PdfPCell(new Phrase(header, headerFont));
                    headerCell.BackgroundColor = new BaseColor(211, 47, 47); // Red color
                    headerCell.HorizontalAlignment = PdfElement.ALIGN_CENTER;
                    headerCell.VerticalAlignment = PdfElement.ALIGN_MIDDLE;
                    headerCell.Padding = 8;
                    summaryTable.AddCell(headerCell);
                }
                
                // Summary values
                var valueFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
                
                var itemsCell = new PdfPCell(new Phrase(LowStockCountLabel.Text, valueFont));
                itemsCell.HorizontalAlignment = PdfElement.ALIGN_CENTER;
                itemsCell.VerticalAlignment = PdfElement.ALIGN_MIDDLE;
                itemsCell.Padding = 8;
                summaryTable.AddCell(itemsCell);
                
                var percentageCell = new PdfPCell(new Phrase(PercentageLabel.Text, valueFont));
                percentageCell.HorizontalAlignment = PdfElement.ALIGN_CENTER;
                percentageCell.VerticalAlignment = PdfElement.ALIGN_MIDDLE;
                percentageCell.Padding = 8;
                summaryTable.AddCell(percentageCell);
                
                document.Add(summaryTable);
                document.Add(new Paragraph(" ")); // Add spacing
                
                // Add low stock items header
                var itemsFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, new BaseColor(211, 47, 47)); // Red color
                var itemsHeader = new Paragraph("Low Stock Items", itemsFont);
                document.Add(itemsHeader);
                document.Add(new Paragraph(" ")); // Add spacing
                
                // Create table for low stock items
                var table = new PdfPTable(5);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 1f, 4f, 1.5f, 1.5f, 3f });
                
                // Add table headers
                string[] headers = { "ID", "Item Name", "Quantity", "Unit", "Supplier" };
                foreach (var header in headers)
                {
                    var headerCell = new PdfPCell(new Phrase(header, headerFont));
                    headerCell.BackgroundColor = new BaseColor(211, 47, 47); // Red color
                    headerCell.HorizontalAlignment = PdfElement.ALIGN_CENTER;
                    headerCell.VerticalAlignment = PdfElement.ALIGN_MIDDLE;
                    headerCell.Padding = 8;
                    table.AddCell(headerCell);
                }
                
                // Add table data
                var cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
                var lowStockFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, PdfFont.BOLD, new BaseColor(211, 47, 47)); // Red color
                var alternateRowColor = new BaseColor(240, 240, 240);
                bool isAlternateRow = false;
                
                // If no items, add a message
                if (_lowStockItems.Count == 0)
                {
                    var emptyCell = new PdfPCell(new Phrase("No items below threshold", cellFont));
                    emptyCell.Colspan = 5;
                    emptyCell.HorizontalAlignment = PdfElement.ALIGN_CENTER;
                    emptyCell.Padding = 8;
                    table.AddCell(emptyCell);
                }
                else 
                {
                    foreach (var item in _lowStockItems)
                    {
                        // ID
                        var idCell = new PdfPCell(new Phrase(item.StockID.ToString(), cellFont));
                        if (isAlternateRow) idCell.BackgroundColor = alternateRowColor;
                        idCell.HorizontalAlignment = PdfElement.ALIGN_CENTER;
                        idCell.VerticalAlignment = PdfElement.ALIGN_MIDDLE;
                        idCell.Padding = 6;
                        table.AddCell(idCell);
                        
                        // Name
                        var nameCell = new PdfPCell(new Phrase(item.Stock_Name, cellFont));
                        if (isAlternateRow) nameCell.BackgroundColor = alternateRowColor;
                        nameCell.HorizontalAlignment = PdfElement.ALIGN_LEFT;
                        nameCell.VerticalAlignment = PdfElement.ALIGN_MIDDLE;
                        nameCell.Padding = 6;
                        table.AddCell(nameCell);
                        
                        // Quantity - red and bold for emphasis
                        var qtyCell = new PdfPCell(new Phrase(item.Quantity.ToString(), lowStockFont));
                        if (isAlternateRow) qtyCell.BackgroundColor = alternateRowColor;
                        qtyCell.HorizontalAlignment = PdfElement.ALIGN_RIGHT;
                        qtyCell.VerticalAlignment = PdfElement.ALIGN_MIDDLE;
                        qtyCell.Padding = 6;
                        table.AddCell(qtyCell);
                        
                        // Unit
                        var unitCell = new PdfPCell(new Phrase(item.Unit, cellFont));
                        if (isAlternateRow) unitCell.BackgroundColor = alternateRowColor;
                        unitCell.HorizontalAlignment = PdfElement.ALIGN_CENTER;
                        unitCell.VerticalAlignment = PdfElement.ALIGN_MIDDLE;
                        unitCell.Padding = 6;
                        table.AddCell(unitCell);
                        
                        // Supplier
                        var supplierCell = new PdfPCell(new Phrase(item.SupplierName, cellFont));
                        if (isAlternateRow) supplierCell.BackgroundColor = alternateRowColor;
                        supplierCell.HorizontalAlignment = PdfElement.ALIGN_LEFT;
                        supplierCell.VerticalAlignment = PdfElement.ALIGN_MIDDLE;
                        supplierCell.Padding = 6;
                        table.AddCell(supplierCell);
                        
                        isAlternateRow = !isAlternateRow;
                    }
                }
                
                document.Add(table);
                
                // Add footer with timestamp
                document.Add(new Paragraph(" ")); // Add spacing
                var footerFont = FontFactory.GetFont(FontFactory.HELVETICA, 8, PdfFont.ITALIC, BaseColor.Gray);
                var footer = new Paragraph($"Generated on {DateTime.Now:MM/dd/yyyy HH:mm:ss}", footerFont);
                footer.Alignment = PdfElement.ALIGN_RIGHT; // Using alias instead of Element
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
                        Title = "Export Low Stock Report PDF",
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
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

public partial class ExpiryReportPage : ContentPage
{
    private readonly StockRepository _stockRepository;
    private ObservableCollection<ExpiryItemViewModel> _expiryItems = new();
    private int _daysThreshold = 30;
    
    public ExpiryReportPage(StockRepository stockRepository)
    {
        InitializeComponent();
        
        _stockRepository = stockRepository;
        
        // Set up collection view
        ExpiryItemsCollection.ItemsSource = _expiryItems;
        
        // Set up days slider
        DaysSlider.Value = _daysThreshold;
        DaysValueLabel.Text = _daysThreshold.ToString();
        DaysSlider.ValueChanged += (s, e) => {
            _daysThreshold = (int)Math.Round(e.NewValue);
            DaysValueLabel.Text = _daysThreshold.ToString();
        };
        
        // Set up event handlers
        ApplyButton.Clicked += OnApplyClicked;
        ExportCsvButton.Clicked += OnExportCsvClicked;
        ExportPdfButton.Clicked += OnExportPdfClicked; // Changed from DisposeButton to ExportPdfButton
        
        // Load data
        LoadExpiryData();
    }
    
    private async void LoadExpiryData()
    {
        try
        {
            // Show loading indicator
            IsBusy = true;
            
            // Get all stocks with expiry dates
            var allStocks = await _stockRepository.GetAllAsync();
            var stocksWithExpiry = allStocks.Where(s => s.ExpiryDate.HasValue).ToList();
            
            // Get items expiring within threshold
            var expiryDate = DateTime.Now.AddDays(_daysThreshold);
            var expiringItems = stocksWithExpiry.Where(s => s.ExpiryDate <= expiryDate).ToList();
            
            // Get counts for various categories
            var today = DateTime.Now.Date;
            var oneWeekFromNow = today.AddDays(7);
            
            int expiringSoonCount = expiringItems.Count;
            int thisWeekCount = expiringItems.Count(s => s.ExpiryDate >= today && s.ExpiryDate <= oneWeekFromNow);
            int expiredCount = expiringItems.Count(s => s.ExpiryDate < today);
            
            // Update summary labels
            MainThread.BeginInvokeOnMainThread(() => {
                ExpiringSoonLabel.Text = expiringSoonCount.ToString();
                ThisWeekLabel.Text = thisWeekCount.ToString();
                ExpiredLabel.Text = expiredCount.ToString();
            });
            
            // Clear and populate items collection
            _expiryItems.Clear();
            foreach (var stock in expiringItems.OrderBy(s => s.ExpiryDate))
            {
                // Calculate days until expiry
                int daysUntilExpiry = 0;
                string expiryColor = "#000000"; // Default black
                
                if (stock.ExpiryDate.HasValue)
                {
                    var timeSpan = stock.ExpiryDate.Value - DateTime.Now.Date;
                    daysUntilExpiry = (int)timeSpan.TotalDays;
                    
                    // Set color based on expiry
                    if (daysUntilExpiry < 0)
                    {
                        expiryColor = "#F44336"; // Red for expired
                    }
                    else if (daysUntilExpiry <= 7)
                    {
                        expiryColor = "#FF9800"; // Orange for soon
                    }
                    else
                    {
                        expiryColor = "#4CAF50"; // Green for OK
                    }
                }
                
                _expiryItems.Add(new ExpiryItemViewModel
                {
                    StockID = stock.StockID,
                    Stock_Name = stock.Stock_Name,
                    ExpiryDate = stock.ExpiryDate,
                    DaysUntilExpiry = daysUntilExpiry < 0 ? "Expired" : $"{daysUntilExpiry} days",
                    Quantity = stock.Quantity,
                    Unit = stock.Unit ?? "",
                    ExpiryColor = expiryColor
                });
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load expiry data: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private void OnApplyClicked(object sender, EventArgs e)
    {
        LoadExpiryData();
    }
    
    private async void OnExportCsvClicked(object sender, EventArgs e)
    {
        try
        {
            // Create CSV content
            var csvContent = new StringBuilder();
            csvContent.AppendLine("Stock ID,Name,Expiry Date,Days Until Expiry,Quantity,Unit");
            
            foreach (var item in _expiryItems)
            {
                csvContent.AppendLine(
                    $"{item.StockID}," +
                    $"{EscapeCsvField(item.Stock_Name)}," +
                    $"{item.ExpiryDate:yyyy-MM-dd}," +
                    $"{EscapeCsvField(item.DaysUntilExpiry)}," +
                    $"{item.Quantity}," +
                    $"{EscapeCsvField(item.Unit)}");
            }
            
            // Generate file name with timestamp
            string fileName = $"Expiry_Report_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
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
                        Title = "Export Expiry Report",
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
            string fileName = $"Expiry_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            
            // Create PDF writer - Using proper disposal with using statement
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                var writer = PdfWriter.GetInstance(document, fileStream);
                document.Open();
                
                // Add title
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
                var title = new Paragraph("Expiry Report", titleFont);
                title.Alignment = PdfElement.ALIGN_CENTER; // Using alias instead of Element
                document.Add(title);
                document.Add(new Paragraph(" ")); // Add spacing
                
                // Add date range information
                var dateFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);
                var dateInfo = new Paragraph(
                    $"Items expiring within {_daysThreshold} days (as of {DateTime.Now:MM/dd/yyyy})", dateFont);
                document.Add(dateInfo);
                document.Add(new Paragraph(" ")); // Add spacing
                
                // Add summary statistics
                var summaryFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
                document.Add(new Paragraph("Summary Statistics:", summaryFont));
                document.Add(new Paragraph($"Expiring Soon: {ExpiringSoonLabel.Text}", dateFont));
                document.Add(new Paragraph($"This Week: {ThisWeekLabel.Text}", dateFont));
                document.Add(new Paragraph($"Already Expired: {ExpiredLabel.Text}", dateFont));
                document.Add(new Paragraph(" ")); // Add spacing
                
                // Create table
                var table = new PdfPTable(5);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 3f, 2f, 2f, 1f, 1f });
                
                // Add table headers
                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.White);
                
                string[] headers = { "Item Name", "Expiry Date", "Days Left", "Quantity", "Unit" };
                foreach (var header in headers)
                {
                    var headerCell = new PdfPCell(new Phrase(header, headerFont));
                    headerCell.BackgroundColor = new BaseColor(76, 175, 80); // Green
                    headerCell.HorizontalAlignment = PdfElement.ALIGN_CENTER;
                    headerCell.VerticalAlignment = PdfElement.ALIGN_MIDDLE;
                    headerCell.Padding = 8;
                    table.AddCell(headerCell);
                }
                
                // Add table data
                var cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
                var alternateRowColor = new BaseColor(240, 240, 240);
                bool isAlternateRow = false;
                
                // If no items, add a message
                if (_expiryItems.Count == 0)
                {
                    var emptyCell = new PdfPCell(new Phrase("No items expiring within the threshold", cellFont));
                    emptyCell.Colspan = 5;
                    emptyCell.HorizontalAlignment = PdfElement.ALIGN_CENTER;
                    table.AddCell(emptyCell);
                }
                else 
                {
                    foreach (var item in _expiryItems)
                    {
                        // Item Name
                        var nameCell = new PdfPCell(new Phrase(item.Stock_Name, cellFont));
                        if (isAlternateRow) nameCell.BackgroundColor = alternateRowColor;
                        nameCell.HorizontalAlignment = PdfElement.ALIGN_LEFT;
                        nameCell.VerticalAlignment = PdfElement.ALIGN_MIDDLE;
                        nameCell.Padding = 6;
                        table.AddCell(nameCell);
                        
                        // Expiry Date
                        var dateCell = new PdfPCell(new Phrase(item.ExpiryDate?.ToString("MM/dd/yyyy") ?? "N/A", cellFont));
                        if (isAlternateRow) dateCell.BackgroundColor = alternateRowColor;
                        dateCell.HorizontalAlignment = PdfElement.ALIGN_CENTER;
                        dateCell.VerticalAlignment = PdfElement.ALIGN_MIDDLE;
                        dateCell.Padding = 6;
                        table.AddCell(dateCell);
                        
                        // Days Left
                        PdfFont daysFont;
                        if (item.DaysUntilExpiry == "Expired")
                        {
                            daysFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, PdfFont.NORMAL, new BaseColor(244, 67, 54)); // Red
                        }
                        else if (item.ExpiryDate?.Date <= DateTime.Now.Date.AddDays(7))
                        {
                            daysFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, PdfFont.NORMAL, new BaseColor(255, 152, 0)); // Orange
                        }
                        else
                        {
                            daysFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, PdfFont.NORMAL, new BaseColor(76, 175, 80)); // Green
                        }
                        
                        var daysCell = new PdfPCell(new Phrase(item.DaysUntilExpiry, daysFont));
                        if (isAlternateRow) daysCell.BackgroundColor = alternateRowColor;
                        daysCell.HorizontalAlignment = PdfElement.ALIGN_CENTER;
                        daysCell.VerticalAlignment = PdfElement.ALIGN_MIDDLE;
                        daysCell.Padding = 6;
                        table.AddCell(daysCell);
                        
                        // Quantity
                        var qtyCell = new PdfPCell(new Phrase(item.Quantity.ToString(), cellFont));
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
                        Title = "Export Expiry Report PDF",
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

// View model for expiry items
public class ExpiryItemViewModel
{
    public int StockID { get; set; }
    public string Stock_Name { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string DaysUntilExpiry { get; set; }
    public double Quantity { get; set; }
    public string Unit { get; set; }
    public string ExpiryColor { get; set; }
}
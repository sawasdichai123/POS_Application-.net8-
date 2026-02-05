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

public partial class SalesSummaryReportPage : ContentPage
{
    private readonly Repository<Order> _orderRepository;
    private readonly Repository<Customer> _customerRepository;
    private ObservableCollection<OrderSummaryViewModel> _orders = new();
    
    public SalesSummaryReportPage(Repository<Order> orderRepository, Repository<Customer> customerRepository)
    {
        InitializeComponent();
        
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        
        // Set up collection view
        OrdersCollection.ItemsSource = _orders;
        
        // Set default date range (last 30 days)
        StartDatePicker.Date = DateTime.Now.AddDays(-30);
        EndDatePicker.Date = DateTime.Now;
        
        // Set up event handlers
        ApplyButton.Clicked += OnApplyClicked;
        ExportCsvButton.Clicked += OnExportCsvClicked;
        ExportPdfButton.Clicked += OnExportPdfClicked;
        
        // Load data
        LoadSalesData();
    }
    
    private async void LoadSalesData()
    {
        try
        {
            // Show loading indicator
            IsBusy = true;
            
            // Get all orders
            var allOrders = await _orderRepository.GetAllAsync();
            
            // Filter by date range
            var startDate = StartDatePicker.Date;
            var endDate = EndDatePicker.Date.AddDays(1).AddSeconds(-1); // End of the selected day
            
            var filteredOrders = allOrders.Where(o => 
                o.OrderDate >= startDate && 
                o.OrderDate <= endDate).ToList();
            
            // Get customers for lookup
            var customers = await _customerRepository.GetAllAsync();
            var customerDict = customers.ToDictionary(c => c.MemberID);
            
            // Calculate statistics
            int totalOrders = filteredOrders.Count;
            double totalRevenue = filteredOrders.Sum(o => o.TotalAmount);
            double avgOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;
            
            // Count by payment status
            int pendingCount = filteredOrders.Count(o => o.PaymentStatus == "Pending");
            int paidCount = filteredOrders.Count(o => o.PaymentStatus == "Paid");
            int cancelledCount = filteredOrders.Count(o => o.PaymentStatus == "Cancelled");
            
            // Update summary labels
            MainThread.BeginInvokeOnMainThread(() => {
                TotalOrdersLabel.Text = totalOrders.ToString();
                TotalRevenueLabel.Text = $"฿{totalRevenue:F2}";
                AvgOrderLabel.Text = $"฿{avgOrderValue:F2}";
                
                PendingCountLabel.Text = pendingCount.ToString();
                PaidCountLabel.Text = paidCount.ToString();
                CancelledCountLabel.Text = cancelledCount.ToString();
            });
            
            // Clear and populate items collection
            _orders.Clear();
            foreach (var order in filteredOrders.OrderByDescending(o => o.OrderDate))
            {
                string customerName = "Guest";
                if (order.MemberID.HasValue && customerDict.TryGetValue(order.MemberID.Value, out var customer))
                {
                    customerName = customer.Cus_Name;
                }
                
                // Set status color
                string statusColor = "#9E9E9E"; // Default gray
                switch (order.PaymentStatus)
                {
                    case "Pending":
                        statusColor = "#FF9800"; // Orange
                        break;
                    case "Paid":
                        statusColor = "#4CAF50"; // Green
                        break;
                    case "Cancelled":
                        statusColor = "#F44336"; // Red
                        break;
                }
                
                _orders.Add(new OrderSummaryViewModel
                {
                    OrderID = order.OrderID,
                    OrderDate = order.OrderDate,
                    CustomerName = customerName,
                    TotalAmount = order.TotalAmount,
                    PaymentStatus = order.PaymentStatus,
                    StatusColor = statusColor
                });
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load sales data: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private void OnApplyClicked(object sender, EventArgs e)
    {
        LoadSalesData();
    }
    
    private async void OnExportCsvClicked(object sender, EventArgs e)
    {
        try
        {
            // Create CSV content
            var csvContent = new StringBuilder();
            csvContent.AppendLine("Order ID,Date,Customer,Amount,Status");
            
            foreach (var order in _orders)
            {
                csvContent.AppendLine(
                    $"{order.OrderID}," +
                    $"{order.OrderDate:yyyy-MM-dd}," +
                    $"{EscapeCsvField(order.CustomerName)}," +
                    $"{order.TotalAmount:F2}," +
                    $"{order.PaymentStatus}");
            }
            
            // Generate file name with timestamp
            string fileName = $"Sales_Summary_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
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
                        Title = "Export Sales Summary",
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
            string fileName = $"Sales_Summary_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            
            // Create PDF writer - Using proper disposal with using statement
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                var writer = PdfWriter.GetInstance(document, fileStream);
                document.Open();
                
                // Add title
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, new BaseColor(76, 175, 80)); // #4CAF50 green
                var title = new Paragraph("Sales Summary Report", titleFont);
                title.Alignment = PdfElement.ALIGN_CENTER;
                document.Add(title);
                document.Add(new Paragraph(" ")); // Add spacing
                
                // Add date range
                var dateRangeFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);
                var dateRange = new Paragraph($"Date Range: {StartDatePicker.Date:MM/dd/yyyy} to {EndDatePicker.Date:MM/dd/yyyy}", dateRangeFont);
                document.Add(dateRange);
                document.Add(new Paragraph(" ")); // Add spacing
                
                // Add summary statistics
                var summaryFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, new BaseColor(76, 175, 80)); // #4CAF50 green
                var summaryHeader = new Paragraph("Summary Statistics", summaryFont);
                document.Add(summaryHeader);
                document.Add(new Paragraph(" ")); // Add spacing
                
                // Create summary table
                var summaryTable = new PdfPTable(3);
                summaryTable.WidthPercentage = 100;
                summaryTable.SetWidths(new float[] { 1f, 1f, 1f });
                
                // Summary table headers
                var summaryHeaderFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.White);
                
                string[] summaryHeaders = { "Total Orders", "Total Revenue", "Average Order Value" };
                foreach (var header in summaryHeaders)
                {
                    var headerCell = new PdfPCell(new Phrase(header, summaryHeaderFont));
                    headerCell.BackgroundColor = new BaseColor(76, 175, 80); // #4CAF50
                    headerCell.HorizontalAlignment = PdfElement.ALIGN_CENTER;
                    headerCell.VerticalAlignment = PdfElement.ALIGN_MIDDLE;
                    headerCell.Padding = 8;
                    summaryTable.AddCell(headerCell);
                }
                
                // Summary values
                var valueFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
                
                var totalOrdersCell = new PdfPCell(new Phrase(TotalOrdersLabel.Text, valueFont));
                totalOrdersCell.HorizontalAlignment = PdfElement.ALIGN_CENTER;
                totalOrdersCell.VerticalAlignment = PdfElement.ALIGN_MIDDLE;
                totalOrdersCell.Padding = 8;
                summaryTable.AddCell(totalOrdersCell);
                
                var totalRevenueCell = new PdfPCell(new Phrase(TotalRevenueLabel.Text, valueFont));
                totalRevenueCell.HorizontalAlignment = PdfElement.ALIGN_CENTER;
                totalRevenueCell.VerticalAlignment = PdfElement.ALIGN_MIDDLE;
                totalRevenueCell.Padding = 8;
                summaryTable.AddCell(totalRevenueCell);
                
                var avgOrderCell = new PdfPCell(new Phrase(AvgOrderLabel.Text, valueFont));
                avgOrderCell.HorizontalAlignment = PdfElement.ALIGN_CENTER;
                avgOrderCell.VerticalAlignment = PdfElement.ALIGN_MIDDLE;
                avgOrderCell.Padding = 8;
                summaryTable.AddCell(avgOrderCell);
                
                document.Add(summaryTable);
                document.Add(new Paragraph(" ")); // Add spacing
                
                // Add order status breakdown
                var statusFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, new BaseColor(76, 175, 80)); // #4CAF50 green
                var statusHeader = new Paragraph("Order Status Breakdown", statusFont);
                document.Add(statusHeader);
                document.Add(new Paragraph(" ")); // Add spacing
                
                // Create status table
                var statusTable = new PdfPTable(3);
                statusTable.WidthPercentage = 100;
                statusTable.SetWidths(new float[] { 1f, 1f, 1f });
                
                // Status table headers with colors
                var statusHeaderFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.White);
                
                // Pending header
                var pendingHeaderCell = new PdfPCell(new Phrase("Pending", statusHeaderFont));
                pendingHeaderCell.BackgroundColor = new BaseColor(255, 152, 0); // #FF9800 - Orange
                pendingHeaderCell.HorizontalAlignment = PdfElement.ALIGN_CENTER;
                pendingHeaderCell.VerticalAlignment = PdfElement.ALIGN_MIDDLE;
                pendingHeaderCell.Padding = 8;
                statusTable.AddCell(pendingHeaderCell);
                
                // Paid header
                var paidHeaderCell = new PdfPCell(new Phrase("Paid", statusHeaderFont));
                paidHeaderCell.BackgroundColor = new BaseColor(76, 175, 80); // #4CAF50 - Green
                paidHeaderCell.HorizontalAlignment = PdfElement.ALIGN_CENTER;
                paidHeaderCell.VerticalAlignment = PdfElement.ALIGN_MIDDLE;
                paidHeaderCell.Padding = 8;
                statusTable.AddCell(paidHeaderCell);
                
                // Cancelled header
                var cancelledHeaderCell = new PdfPCell(new Phrase("Cancelled", statusHeaderFont));
                cancelledHeaderCell.BackgroundColor = new BaseColor(244, 67, 54); // #F44336 - Red
                cancelledHeaderCell.HorizontalAlignment = PdfElement.ALIGN_CENTER;
                cancelledHeaderCell.VerticalAlignment = PdfElement.ALIGN_MIDDLE;
                cancelledHeaderCell.Padding = 8;
                statusTable.AddCell(cancelledHeaderCell);
                
                // Status values
                var pendingCell = new PdfPCell(new Phrase(PendingCountLabel.Text, valueFont));
                pendingCell.HorizontalAlignment = PdfElement.ALIGN_CENTER;
                pendingCell.VerticalAlignment = PdfElement.ALIGN_MIDDLE;
                pendingCell.Padding = 8;
                statusTable.AddCell(pendingCell);
                
                var paidCell = new PdfPCell(new Phrase(PaidCountLabel.Text, valueFont));
                paidCell.HorizontalAlignment = PdfElement.ALIGN_CENTER;
                paidCell.VerticalAlignment = PdfElement.ALIGN_MIDDLE;
                paidCell.Padding = 8;
                statusTable.AddCell(paidCell);
                
                var cancelledCell = new PdfPCell(new Phrase(CancelledCountLabel.Text, valueFont));
                cancelledCell.HorizontalAlignment = PdfElement.ALIGN_CENTER;
                cancelledCell.VerticalAlignment = PdfElement.ALIGN_MIDDLE;
                cancelledCell.Padding = 8;
                statusTable.AddCell(cancelledCell);
                
                document.Add(statusTable);
                document.Add(new Paragraph(" ")); // Add spacing
                
                // Add orders table header
                var ordersFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, new BaseColor(76, 175, 80)); // #4CAF50 green
                var ordersHeader = new Paragraph("Order Details", ordersFont);
                document.Add(ordersHeader);
                document.Add(new Paragraph(" ")); // Add spacing
                
                // Create orders table
                var ordersTable = new PdfPTable(5);
                ordersTable.WidthPercentage = 100;
                ordersTable.SetWidths(new float[] { 1f, 2f, 3f, 2f, 2f });
                
                // Orders table headers
                var ordersHeaderFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.White);
                
                string[] ordersHeaders = { "Order #", "Date", "Customer", "Amount", "Status" };
                foreach (var header in ordersHeaders)
                {
                    var headerCell = new PdfPCell(new Phrase(header, ordersHeaderFont));
                    headerCell.BackgroundColor = new BaseColor(76, 175, 80); // #4CAF50
                    headerCell.HorizontalAlignment = PdfElement.ALIGN_CENTER;
                    headerCell.VerticalAlignment = PdfElement.ALIGN_MIDDLE;
                    headerCell.Padding = 8;
                    ordersTable.AddCell(headerCell);
                }
                
                // Orders data rows
                var regularFont = FontFactory.GetFont(FontFactory.HELVETICA, 11);
                var alternateRowColor = new BaseColor(240, 240, 240);
                bool isAlternateRow = false;
                
                foreach (var order in _orders)
                {
                    // Order ID
                    var cell = new PdfPCell(new Phrase(order.OrderID.ToString(), regularFont));
                    if (isAlternateRow)
                        cell.BackgroundColor = alternateRowColor;
                    cell.HorizontalAlignment = PdfElement.ALIGN_CENTER;
                    cell.VerticalAlignment = PdfElement.ALIGN_MIDDLE;
                    cell.Padding = 6;
                    ordersTable.AddCell(cell);
                    
                    // Date
                    cell = new PdfPCell(new Phrase(order.OrderDate.ToString("MM/dd/yyyy"), regularFont));
                    if (isAlternateRow)
                        cell.BackgroundColor = alternateRowColor;
                    cell.HorizontalAlignment = PdfElement.ALIGN_CENTER;
                    cell.VerticalAlignment = PdfElement.ALIGN_MIDDLE;
                    cell.Padding = 6;
                    ordersTable.AddCell(cell);
                    
                    // Customer
                    cell = new PdfPCell(new Phrase(order.CustomerName, regularFont));
                    if (isAlternateRow)
                        cell.BackgroundColor = alternateRowColor;
                    cell.HorizontalAlignment = PdfElement.ALIGN_LEFT;
                    cell.VerticalAlignment = PdfElement.ALIGN_MIDDLE;
                    cell.Padding = 6;
                    ordersTable.AddCell(cell);
                    
                    // Amount
                    cell = new PdfPCell(new Phrase($"฿{order.TotalAmount:F2}", regularFont));
                    if (isAlternateRow)
                        cell.BackgroundColor = alternateRowColor;
                    cell.HorizontalAlignment = PdfElement.ALIGN_RIGHT;
                    cell.VerticalAlignment = PdfElement.ALIGN_MIDDLE;
                    cell.Padding = 6;
                    ordersTable.AddCell(cell);
                    
                    // Status with colored text
                    PdfFont statusCellFont;
                    switch (order.PaymentStatus)
                    {
                        case "Pending":
                            statusCellFont = FontFactory.GetFont(FontFactory.HELVETICA, 11, new BaseColor(255, 152, 0)); // Orange
                            break;
                        case "Paid":
                            statusCellFont = FontFactory.GetFont(FontFactory.HELVETICA, 11, new BaseColor(76, 175, 80)); // Green
                            break;
                        case "Cancelled":
                            statusCellFont = FontFactory.GetFont(FontFactory.HELVETICA, 11, new BaseColor(244, 67, 54)); // Red
                            break;
                        default:
                            statusCellFont = regularFont;
                            break;
                    }
                    
                    cell = new PdfPCell(new Phrase(order.PaymentStatus, statusCellFont));
                    if (isAlternateRow)
                        cell.BackgroundColor = alternateRowColor;
                    cell.HorizontalAlignment = PdfElement.ALIGN_CENTER;
                    cell.VerticalAlignment = PdfElement.ALIGN_MIDDLE;
                    cell.Padding = 6;
                    ordersTable.AddCell(cell);
                    
                    isAlternateRow = !isAlternateRow;
                }
                
                document.Add(ordersTable);
                
                // Add footer with timestamp
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
                        Title = "Export Sales Summary as PDF",
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

// View model for order summary
public class OrderSummaryViewModel
{
    public int OrderID { get; set; }
    public DateTime OrderDate { get; set; }
    public string CustomerName { get; set; }
    public double TotalAmount { get; set; }
    public string PaymentStatus { get; set; }
    public string StatusColor { get; set; }
}
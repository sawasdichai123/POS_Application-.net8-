using System.Collections.ObjectModel;
using StockApplication.Models;
using StockApplication.Repositories;

namespace StockApplication.Views;

public partial class MenuItemListPage : ContentPage
{
    private readonly Repository<MenuItem> _menuItemRepository;
    private readonly Repository<OrderDetail> _orderDetailRepository;
    private ObservableCollection<MenuItem> _menuItems = new();
    private string _currentCategory = "All";

    public MenuItemListPage(Repository<MenuItem> menuItemRepository, Repository<OrderDetail> orderDetailRepository)
    {
        InitializeComponent();
        
        _menuItemRepository = menuItemRepository;
        _orderDetailRepository = orderDetailRepository;
        
        // Set up collection view
        MenuItemsCollection.ItemsSource = _menuItems;
        
        // Set up refresh view
        MenuRefreshView.Command = new Command(async () => await LoadMenuItemsAsync(_currentCategory));
        
        // Set up event handlers
        AddMenuItemButton.Clicked += OnAddMenuItemClicked;
        SearchEntry.TextChanged += OnSearchTextChanged;
        MenuItemsCollection.SelectionChanged += OnMenuItemSelectionChanged;
        
        // Category filters
        AllCategoryButton.Clicked += (s, e) => LoadMenuItemsAsync("All");
        MainCategoryButton.Clicked += (s, e) => LoadMenuItemsAsync("Main");
        SideCategoryButton.Clicked += (s, e) => LoadMenuItemsAsync("Side");
        DessertCategoryButton.Clicked += (s, e) => LoadMenuItemsAsync("Dessert");
        BeverageCategoryButton.Clicked += (s, e) => LoadMenuItemsAsync("Beverage");
        OtherCategoryButton.Clicked += (s, e) => LoadMenuItemsAsync("Other");
        
        // Sort button
        FilterButton.Clicked += OnFilterClicked;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Load menu items
        await LoadMenuItemsAsync("All");
    }

    private async Task LoadMenuItemsAsync(string category)
    {
        try
        {
            // Update button styles
            UpdateCategoryButtonStyles(category);
            
            // Store current category
            _currentCategory = category;
            
            MenuRefreshView.IsRefreshing = true;
            
            // Get menu items
            var menuItems = await _menuItemRepository.GetAllAsync();
            
            // Apply category filter if not "All"
            if (category != "All")
            {
                menuItems = menuItems.Where(i => i.Category == category).ToList();
            }
            
            // Update collection
            _menuItems.Clear();
            foreach (var item in menuItems)
            {
                _menuItems.Add(item);
            }
            
            // Update total count
            TotalItemsLabel.Text = $"Total Menu Items: {_menuItems.Count}";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load menu items: {ex.Message}", "OK");
        }
        finally
        {
            MenuRefreshView.IsRefreshing = false;
        }
    }

    private void UpdateCategoryButtonStyles(string selectedCategory)
    {
        // Reset all buttons
        AllCategoryButton.BackgroundColor = Color.Parse("#E0E0E0");
        AllCategoryButton.TextColor = Color.Parse("#616161");
        
        MainCategoryButton.BackgroundColor = Color.Parse("#E0E0E0");
        MainCategoryButton.TextColor = Color.Parse("#616161");
        
        SideCategoryButton.BackgroundColor = Color.Parse("#E0E0E0");
        SideCategoryButton.TextColor = Color.Parse("#616161");
        
        DessertCategoryButton.BackgroundColor = Color.Parse("#E0E0E0");
        DessertCategoryButton.TextColor = Color.Parse("#616161");
        
        BeverageCategoryButton.BackgroundColor = Color.Parse("#E0E0E0");
        BeverageCategoryButton.TextColor = Color.Parse("#616161");
        
        OtherCategoryButton.BackgroundColor = Color.Parse("#E0E0E0");
        OtherCategoryButton.TextColor = Color.Parse("#616161");
        
        // Set selected button
        Color selectedColor = Color.Parse("#3F51B5");
        Color selectedTextColor = Colors.White;
        
        switch (selectedCategory)
        {
            case "All":
                AllCategoryButton.BackgroundColor = selectedColor;
                AllCategoryButton.TextColor = selectedTextColor;
                break;
            case "Main":
                MainCategoryButton.BackgroundColor = selectedColor;
                MainCategoryButton.TextColor = selectedTextColor;
                break;
            case "Side":
                SideCategoryButton.BackgroundColor = selectedColor;
                SideCategoryButton.TextColor = selectedTextColor;
                break;
            case "Dessert":
                DessertCategoryButton.BackgroundColor = selectedColor;
                DessertCategoryButton.TextColor = selectedTextColor;
                break;
            case "Beverage":
                BeverageCategoryButton.BackgroundColor = selectedColor;
                BeverageCategoryButton.TextColor = selectedTextColor;
                break;
            case "Other":
                OtherCategoryButton.BackgroundColor = selectedColor;
                OtherCategoryButton.TextColor = selectedTextColor;
                break;
        }
    }

    private async void OnAddMenuItemClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("Menu/Add");
    }

    private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.NewTextValue))
        {
            await LoadMenuItemsAsync(_currentCategory);
            return;
        }
        
        var searchTerm = e.NewTextValue.ToLowerInvariant();
        var allMenuItems = await _menuItemRepository.GetAllAsync();
        
        // Apply category filter if not "All"
        if (_currentCategory != "All")
        {
            allMenuItems = allMenuItems.Where(i => i.Category == _currentCategory).ToList();
        }
        
        // Apply search filter
        var filteredItems = allMenuItems.Where(i => 
            i.Menu_Name.ToLowerInvariant().Contains(searchTerm) ||
            (i.Description != null && i.Description.ToLowerInvariant().Contains(searchTerm)));
        
        _menuItems.Clear();
        foreach (var item in filteredItems)
        {
            _menuItems.Add(item);
        }
        
        TotalItemsLabel.Text = $"Search Results: {_menuItems.Count}";
    }

    private async void OnMenuItemSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is MenuItem selectedItem)
        {
            // Navigate to edit page with the selected item ID
            MenuItemsCollection.SelectedItem = null;
            await Shell.Current.GoToAsync($"Menu/Edit?id={selectedItem.MenuID}");
        }
    }
    
    private async void OnEditMenuItemClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is MenuItem menuItem)
        {
            // Navigate to edit page with the selected item ID
            await Shell.Current.GoToAsync($"Menu/Edit?id={menuItem.MenuID}");
        }
    }
    
    private async void OnDeleteMenuItemClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is MenuItem menuItem)
        {
            // Check if the menu item is used in any orders
            var orderDetails = await _orderDetailRepository.FindAsync(od => od.MenuID == menuItem.MenuID);
            
            if (orderDetails.Any())
            {
                // Menu item is used in orders, show warning
                bool confirm = await DisplayAlert("Warning", 
                    $"'{menuItem.Menu_Name}' is used in {orderDetails.Count} orders. Deleting this menu item may affect existing orders. Continue?", 
                    "Yes", "No");
                    
                if (!confirm)
                    return;
            }
            else
            {
                // Simple confirmation
                bool confirm = await DisplayAlert("Delete Menu Item", 
                    $"Are you sure you want to delete '{menuItem.Menu_Name}'?", 
                    "Yes", "No");
                    
                if (!confirm)
                    return;
            }
            
            try
            {
                // Delete the menu item
                await _menuItemRepository.DeleteAsync(menuItem);
                
                // Refresh the list
                await LoadMenuItemsAsync(_currentCategory);
                
                // Show success message
                await DisplayAlert("Success", "Menu item deleted successfully.", "OK");
            }
            catch (Exception ex)
            {
                // Show error message
                await DisplayAlert("Error", $"Failed to delete menu item: {ex.Message}", "OK");
            }
        }
    }
    
    private async void OnFilterClicked(object sender, EventArgs e)
    {
        string action = await DisplayActionSheet("Sort By", "Cancel", null, 
            "Name (A-Z)", "Name (Z-A)", "Price (Low to High)", "Price (High to Low)");
        
        if (action == "Cancel" || string.IsNullOrEmpty(action))
            return;
            
        List<MenuItem> sortedItems = _menuItems.ToList();
        
        switch (action)
        {
            case "Name (A-Z)":
                sortedItems = sortedItems.OrderBy(i => i.Menu_Name).ToList();
                break;
                
            case "Name (Z-A)":
                sortedItems = sortedItems.OrderByDescending(i => i.Menu_Name).ToList();
                break;
                
            case "Price (Low to High)":
                sortedItems = sortedItems.OrderBy(i => i.Price).ToList();
                break;
                
            case "Price (High to Low)":
                sortedItems = sortedItems.OrderByDescending(i => i.Price).ToList();
                break;
        }
        
        _menuItems.Clear();
        foreach (var item in sortedItems)
        {
            _menuItems.Add(item);
        }
    }
}
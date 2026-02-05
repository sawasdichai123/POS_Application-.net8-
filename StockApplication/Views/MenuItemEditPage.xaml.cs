using StockApplication.Repositories;

namespace StockApplication.Views;

[QueryProperty(nameof(MenuItemId), "id")]
public partial class MenuItemEditPage : ContentPage
{
    private readonly Repository<MenuItem> _menuItemRepository;
    private int _menuItemId;
    private bool _isNewMenuItem = true;

    public int MenuItemId
    {
        get => _menuItemId;
        set
        {
            _menuItemId = value;
            LoadMenuItem();
        }
    }

    public MenuItemEditPage(Repository<MenuItem> menuItemRepository)
    {
        InitializeComponent();
        
        _menuItemRepository = menuItemRepository;
        
        // Set up event handlers
        SaveButton.Clicked += OnSaveClicked;
        CancelButton.Clicked += OnCancelClicked;
    }

    private async void LoadMenuItem()
    {
        if (_menuItemId <= 0)
        {
            // New menu item
            _isNewMenuItem = true;
            PageTitleLabel.Text = "Add New Menu Item";
            Title = "Add Menu Item";
            
            // Set default category
            CategoryPicker.SelectedIndex = 0; // Main
            return;
        }
        
        try
        {
            // Existing menu item
            _isNewMenuItem = false;
            PageTitleLabel.Text = "Edit Menu Item";
            Title = "Edit Menu Item";
            
            var menuItem = await _menuItemRepository.GetByIdAsync(_menuItemId);
            if (menuItem != null)
            {
                // Populate the form
                MenuNameEntry.Text = menuItem.Menu_Name;
                PriceEntry.Text = menuItem.Price.ToString();
                DescriptionEditor.Text = menuItem.Description;
                
                // Set category
                if (!string.IsNullOrEmpty(menuItem.Category))
                {
                    var categoryItems = CategoryPicker.ItemsSource as IList<string>;
                    if (categoryItems != null)
                    {
                        int categoryIndex = categoryItems.IndexOf(menuItem.Category);
                        if (categoryIndex >= 0)
                        {
                            CategoryPicker.SelectedIndex = categoryIndex;
                        }
                        else
                        {
                            // If category not found, default to "Other"
                            int otherIndex = categoryItems.IndexOf("Other");
                            if (otherIndex >= 0)
                            {
                                CategoryPicker.SelectedIndex = otherIndex;
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load menu item: {ex.Message}", "OK");
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(MenuNameEntry.Text))
        {
            await DisplayAlert("Validation Error", "Menu item name is required.", "OK");
            return;
        }
        
        if (CategoryPicker.SelectedIndex < 0)
        {
            await DisplayAlert("Validation Error", "Please select a category.", "OK");
            return;
        }
        
        if (string.IsNullOrWhiteSpace(PriceEntry.Text) || !double.TryParse(PriceEntry.Text, out double price))
        {
            await DisplayAlert("Validation Error", "Please enter a valid price.", "OK");
            return;
        }
        
        try
        {
            // Create menu item object
            var menuItem = new MenuItem
            {
                MenuID = _menuItemId,
                Menu_Name = MenuNameEntry.Text.Trim(),
                Category = CategoryPicker.SelectedItem.ToString(),
                Description = DescriptionEditor.Text?.Trim(),
                Price = price
            };
            
            if (_isNewMenuItem)
            {
                // Insert new menu item
                await _menuItemRepository.InsertAsync(menuItem);
                await DisplayAlert("Success", "Menu item added successfully.", "OK");
            }
            else
            {
                // Update existing menu item
                await _menuItemRepository.UpdateAsync(menuItem);
                await DisplayAlert("Success", "Menu item updated successfully.", "OK");
            }
            
            // Navigate back
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to save menu item: {ex.Message}", "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        // Confirm cancel if there are changes
        bool hasChanges = !string.IsNullOrEmpty(MenuNameEntry.Text) || 
                         !string.IsNullOrEmpty(PriceEntry.Text) ||
                         !string.IsNullOrEmpty(DescriptionEditor.Text) ||
                         CategoryPicker.SelectedIndex >= 0;
        
        if (hasChanges)
        {
            bool cancel = await DisplayAlert("Confirm", 
                "Are you sure you want to cancel? Any unsaved changes will be lost.", 
                "Yes", "No");
            
            if (!cancel)
                return;
        }
        
        // Navigate back
        await Shell.Current.GoToAsync("..");
    }
}
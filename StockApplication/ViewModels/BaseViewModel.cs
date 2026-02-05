using CommunityToolkit.Mvvm.ComponentModel;

namespace StockApplication.ViewModels
{
    // Base ViewModel with common functionality
    public abstract class BaseViewModel : ObservableObject
    {
        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        private string _title;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }
    }
}
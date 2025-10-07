using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using VenueIQ.App.Services;
using VenueIQ.App.Utils;

namespace VenueIQ.App.ViewModels;

public class StartupViewModel : INotifyPropertyChanged
{
    private readonly SettingsService _settings;
    private readonly PoiSearchService _poi;

    public StartupViewModel(SettingsService settings, PoiSearchService poi)
    {
        _settings = settings;
        _poi = poi;
        TestAndSaveCommand = new AsyncCommand(ExecuteTestAndSaveAsync, CanExecuteTestAndSave);
    }

    private string? _apiKey;
    public string? ApiKey
    {
        get => _apiKey;
        set { if (_apiKey != value) { _apiKey = value; OnPropertyChanged(); ((AsyncCommand)TestAndSaveCommand).RaiseCanExecuteChanged(); } }
    }

    private bool _isBusy;
    public bool IsBusy { get => _isBusy; set { if (_isBusy != value) { _isBusy = value; OnPropertyChanged(); ((AsyncCommand)TestAndSaveCommand).RaiseCanExecuteChanged(); } } }

    private bool _isSuccess;
    public bool IsSuccess { get => _isSuccess; set { if (_isSuccess != value) { _isSuccess = value; OnPropertyChanged(); } } }

    private string? _errorMessage;
    public string? ErrorMessage { get => _errorMessage; set { if (_errorMessage != value) { _errorMessage = value; OnPropertyChanged(); HasError = !string.IsNullOrWhiteSpace(_errorMessage); } } }
    private bool _hasError;
    public bool HasError { get => _hasError; set { if (_hasError != value) { _hasError = value; OnPropertyChanged(); } } }

    public ICommand TestAndSaveCommand { get; }

    private bool CanExecuteTestAndSave() => !IsBusy && !string.IsNullOrWhiteSpace(ApiKey);

    private async Task ExecuteTestAndSaveAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        IsSuccess = false;
        ErrorMessage = null;
        try
        {
            var ok = await _poi.TestApiKeyAsync(ApiKey!).ConfigureAwait(false);
            if (!ok)
            {
                ErrorMessage = Helpers.LocalizationResourceManager.Instance["Startup_Error_InvalidKey"];
                return;
            }
            await _settings.SetApiKeyAsync(ApiKey!).ConfigureAwait(false);
            IsSuccess = true;
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Shell.Current.GoToAsync("//Main");
            });
        }
        catch
        {
            ErrorMessage = Helpers.LocalizationResourceManager.Instance["Startup_Error_Connection"];
        }
        finally
        {
            IsBusy = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

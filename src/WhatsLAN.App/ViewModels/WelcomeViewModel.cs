using System.Windows.Input;
using WhatsLAN.App.MVVM;
using WhatsLAN.Core.Models;
using WhatsLAN.Core.Services;

namespace WhatsLAN.App.ViewModels;

public class WelcomeViewModel : ObservableObject
{
    private readonly ProfileService _profileService;
    private string _pseudo = string.Empty;
    private string _status = "Disponible";
    private string? _avatarPath;
    private string? _errorMessage;
    private bool _hasError;

    public event Action<Profile>? ProfileCreated;

    public string Pseudo
    {
        get => _pseudo;
        set
        {
            if (SetProperty(ref _pseudo, value))
            {
                Validate();
                OnPropertyChanged(nameof(CanSubmit));
            }
        }
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public string? AvatarPath
    {
        get => _avatarPath;
        set => SetProperty(ref _avatarPath, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool HasError
    {
        get => _hasError;
        set => SetProperty(ref _hasError, value);
    }

    public bool CanSubmit => !string.IsNullOrWhiteSpace(_pseudo) && _pseudo.Trim().Length <= 25;

    public ICommand SubmitCommand { get; }

    public WelcomeViewModel(ProfileService profileService)
    {
        _profileService = profileService;
        SubmitCommand = new RelayCommand(Submit, () => CanSubmit);
    }

    private void Validate()
    {
        var trimmed = _pseudo.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            ErrorMessage = "Entrez un pseudo (25 caractères max).";
            HasError = true;
        }
        else if (trimmed.Length > 25)
        {
            ErrorMessage = "Le pseudo ne doit pas dépasser 25 caractères.";
            HasError = true;
        }
        else
        {
            ErrorMessage = null;
            HasError = false;
        }
    }

    private void Submit()
    {
        Validate();
        if (HasError || !CanSubmit) return;

        var profile = _profileService.CreateProfile(Pseudo, Status, AvatarPath);
        ProfileCreated?.Invoke(profile);
    }
}

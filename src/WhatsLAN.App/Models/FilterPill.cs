using WhatsLAN.App.MVVM;

namespace WhatsLAN.App.Models;

public class FilterPill : ObservableObject
{
    private string _name = string.Empty;
    private int _count;
    private bool _isSelected;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public int Count
    {
        get => _count;
        set
        {
            if (SetProperty(ref _count, value))
            {
                OnPropertyChanged(nameof(HasCount));
            }
        }
    }

    public bool HasCount => _count > 0;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}

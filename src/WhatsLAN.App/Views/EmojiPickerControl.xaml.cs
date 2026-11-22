using System.Windows;
using System.Windows.Controls;
using WhatsLAN.Core.Services;

namespace WhatsLAN.App.Views;

public partial class EmojiPickerControl : UserControl
{
    public event Action<string>? EmojiSelected;

    private readonly EmojiService _emojiService = EmojiService.Instance;
    private string _currentCategory = "Récents";

    public EmojiPickerControl()
    {
        InitializeComponent();
        CategoriesItemsControl.ItemsSource = _emojiService.Categories;
        LoadCategory(_currentCategory);
    }

    public void RefreshRecents()
    {
        if (_currentCategory == "Récents")
        {
            LoadCategory("Récents");
        }
    }

    private void LoadCategory(string category)
    {
        _currentCategory = category;
        SearchBox.Text = string.Empty;
        EmojisItemsControl.ItemsSource = _emojiService.GetEmojisByCategory(category);
    }

    private void CategoryButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Content is string category)
        {
            LoadCategory(category);
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var text = SearchBox.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            LoadCategory(_currentCategory);
        }
        else
        {
            EmojisItemsControl.ItemsSource = _emojiService.Search(text);
        }
    }

    private void EmojiButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Content is string emoji)
        {
            _emojiService.AddRecent(emoji);
            EmojiSelected?.Invoke(emoji);
        }
    }
}

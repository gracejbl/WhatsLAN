using System.Collections.ObjectModel;
using System.Windows.Input;
using WhatsLAN.App.Models;
using WhatsLAN.App.MVVM;
using WhatsLAN.Core.Models;
using WhatsLAN.Core.Storage;

namespace WhatsLAN.App.ViewModels;

public class NewChatViewModel : ObservableObject
{
    private readonly ContactRepository _contactRepository;
    private readonly ConversationRepository _conversationRepository;
    private string _searchText = string.Empty;
    private Contact? _selectedContact;
    private readonly List<Contact> _allContacts = [];

    public ObservableCollection<Contact> FilteredContacts { get; } = [];

    public event Action<string>? ContactSelected;

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilter();
            }
        }
    }

    public Contact? SelectedContact
    {
        get => _selectedContact;
        set
        {
            if (SetProperty(ref _selectedContact, value) && value != null)
            {
                SelectContact(value);
            }
        }
    }

    public ICommand CreateManualContactCommand { get; }

    public NewChatViewModel(ContactRepository contactRepository, ConversationRepository conversationRepository)
    {
        _contactRepository = contactRepository;
        _conversationRepository = conversationRepository;

        CreateManualContactCommand = new RelayCommand(CreateManualContact);

        LoadContacts();
    }

    private void LoadContacts()
    {
        _allContacts.Clear();
        FilteredContacts.Clear();

        var contacts = _contactRepository.GetAll();
        // Trier : en ligne en premier, puis alphabétique (§7.1)
        var sorted = contacts.OrderByDescending(c => c.IsOnline).ThenBy(c => c.Name);

        foreach (var c in sorted)
        {
            _allContacts.Add(c);
            FilteredContacts.Add(c);
        }
    }

    private void ApplyFilter()
    {
        FilteredContacts.Clear();
        var query = _allContacts.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            query = query.Where(c => c.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                                     c.Status.Contains(_searchText, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var c in query)
        {
            FilteredContacts.Add(c);
        }
    }

    private void SelectContact(Contact contact)
    {
        // Créer ou retrouver la conversation
        var existingConv = _conversationRepository.GetAll().FirstOrDefault(c => c.Id == contact.UserId || c.PeerUserId == contact.Name);
        if (existingConv == null)
        {
            existingConv = new Conversation
            {
                Id = contact.UserId,
                Type = "direct",
                PeerUserId = contact.Name,
                Pinned = false,
                Muted = false,
                Archived = false,
                UnreadCount = 0,
                UpdatedUtc = DateTime.UtcNow
            };
            _conversationRepository.Save(existingConv);
        }

        ContactSelected?.Invoke(existingConv.Id);
    }

    private void CreateManualContact()
    {
        if (string.IsNullOrWhiteSpace(_searchText)) return;

        var name = _searchText.Trim();
        var id = Guid.NewGuid().ToString();

        var contact = new Contact
        {
            UserId = id,
            Name = name,
            Status = "Disponible",
            IsOnline = false
        };
        _contactRepository.Save(contact);

        SelectContact(contact);
    }
}

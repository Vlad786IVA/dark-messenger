using DARK_Messenger.Services;
using ContactModel = DARK_Messenger.Models.Contact;

namespace DARK_Messenger.Views;

public partial class ContactsPage : ContentPage
{
    private readonly ApiClient _api;
    private readonly SettingsService _settings;

    public ContactsPage(ApiClient api, SettingsService settings)
    {
        InitializeComponent();
        _api = api;
        _settings = settings;
        BindingContext = this;
        LoadContacts();
    }

    public System.Collections.ObjectModel.ObservableCollection<ContactModel> Contacts { get; } = new();

    private async void LoadContacts()
    {
        var contacts = await _api.GetContactsAsync();
        if (contacts != null)
        {
            Contacts.Clear();
            foreach (var c in contacts) Contacts.Add(c);
        }
    }

    private async void OnContactSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is ContactModel contact)
        {
            var chatId = await _api.CreateChatAsync(contact.UserId);
            if (!string.IsNullOrEmpty(chatId))
            {
                await Shell.Current.GoToAsync($"//Chat?chatId={chatId}");
            }
        }
        ((CollectionView)sender).SelectedItem = null;
    }

    private async void OnAddContact(object sender, EventArgs e)
    {
        var phone = await DisplayPromptAsync("Добавить контакт", "Номер телефона:", placeholder: "+7");
        if (!string.IsNullOrEmpty(phone))
        {
            // Search and add contact logic
            await DisplayAlert("Инфо", "Контакт добавлен (демо)", "OK");
        }
    }
}

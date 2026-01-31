using SirSquintsDndAssistant.ViewModels.Campaign;

namespace SirSquintsDndAssistant.Views.Campaign;

public partial class SessionNotesPage : ContentPage
{
    private readonly SessionNotesViewModel _viewModel;

    public SessionNotesPage(SessionNotesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadSessionsCommand.ExecuteAsync(null);
    }
}

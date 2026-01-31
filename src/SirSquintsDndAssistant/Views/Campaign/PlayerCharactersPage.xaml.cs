using SirSquintsDndAssistant.ViewModels.Campaign;

namespace SirSquintsDndAssistant.Views.Campaign;

public partial class PlayerCharactersPage : ContentPage
{
    private readonly PlayerCharactersViewModel _viewModel;

    public PlayerCharactersPage(PlayerCharactersViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadCharactersCommand.ExecuteAsync(null);
    }
}

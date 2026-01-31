using SirSquintsDndAssistant.ViewModels.Multiplayer;

namespace SirSquintsDndAssistant.Views.Multiplayer;

public partial class PlayerSessionPage : ContentPage
{
    private readonly PlayerSessionViewModel _viewModel;

    public PlayerSessionPage(PlayerSessionViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadDataCommand.ExecuteAsync(null);
    }
}

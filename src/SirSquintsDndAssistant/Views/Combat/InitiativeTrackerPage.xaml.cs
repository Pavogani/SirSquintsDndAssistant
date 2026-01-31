using SirSquintsDndAssistant.ViewModels.Combat;

namespace SirSquintsDndAssistant.Views.Combat;

public partial class InitiativeTrackerPage : ContentPage
{
    private readonly InitiativeTrackerViewModel _viewModel;

    public InitiativeTrackerPage(InitiativeTrackerViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadCombatCommand.ExecuteAsync(null);
    }
}

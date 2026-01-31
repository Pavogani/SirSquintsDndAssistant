using SirSquintsDndAssistant.ViewModels.Encounter;

namespace SirSquintsDndAssistant.Views.Encounter;

public partial class EncounterBuilderPage : ContentPage
{
    private readonly EncounterBuilderViewModel _viewModel;

    public EncounterBuilderPage(EncounterBuilderViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadMonstersCommand.ExecuteAsync(null);
    }
}

using SirSquintsDndAssistant.ViewModels.Encounter;

namespace SirSquintsDndAssistant.Views.Encounter;

public partial class EncounterLibraryPage : ContentPage
{
    public EncounterLibraryPage(EncounterLibraryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is EncounterLibraryViewModel vm)
        {
            vm.LoadEncountersCommand.Execute(null);
        }
    }
}

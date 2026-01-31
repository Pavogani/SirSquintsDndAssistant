using SirSquintsDndAssistant.ViewModels.Reference;

namespace SirSquintsDndAssistant.Views.Reference;

public partial class SpellbookPage : ContentPage
{
    private readonly SpellbookViewModel _viewModel;

    public SpellbookPage(SpellbookViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadSpellsCommand.ExecuteAsync(null);
    }
}

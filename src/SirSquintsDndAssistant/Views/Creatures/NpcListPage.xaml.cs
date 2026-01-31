using SirSquintsDndAssistant.ViewModels.Creatures;

namespace SirSquintsDndAssistant.Views.Creatures;

public partial class NpcListPage : ContentPage
{
    private readonly NpcListViewModel _viewModel;

    public NpcListPage(NpcListViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadNpcsCommand.ExecuteAsync(null);
    }
}

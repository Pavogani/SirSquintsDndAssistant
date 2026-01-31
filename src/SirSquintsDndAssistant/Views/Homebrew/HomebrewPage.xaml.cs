using SirSquintsDndAssistant.ViewModels.Homebrew;

namespace SirSquintsDndAssistant.Views.Homebrew;

public partial class HomebrewPage : ContentPage
{
    private readonly HomebrewViewModel _viewModel;

    public HomebrewPage(HomebrewViewModel viewModel)
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

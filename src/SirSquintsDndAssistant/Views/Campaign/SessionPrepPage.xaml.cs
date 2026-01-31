using SirSquintsDndAssistant.ViewModels.Campaign;

namespace SirSquintsDndAssistant.Views.Campaign;

public partial class SessionPrepPage : ContentPage
{
    private readonly SessionPrepViewModel _viewModel;

    public SessionPrepPage(SessionPrepViewModel viewModel)
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

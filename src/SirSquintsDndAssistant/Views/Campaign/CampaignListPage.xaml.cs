using SirSquintsDndAssistant.ViewModels.Campaign;

namespace SirSquintsDndAssistant.Views.Campaign;

public partial class CampaignListPage : ContentPage
{
    private readonly CampaignListViewModel _viewModel;

    public CampaignListPage(CampaignListViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadCampaignsCommand.ExecuteAsync(null);
    }
}

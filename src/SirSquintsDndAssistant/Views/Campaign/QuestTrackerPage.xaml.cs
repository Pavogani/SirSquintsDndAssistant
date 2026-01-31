using SirSquintsDndAssistant.ViewModels.Campaign;

namespace SirSquintsDndAssistant.Views.Campaign;

public partial class QuestTrackerPage : ContentPage
{
    private readonly QuestTrackerViewModel _viewModel;

    public QuestTrackerPage(QuestTrackerViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadQuestsCommand.ExecuteAsync(null);
    }
}

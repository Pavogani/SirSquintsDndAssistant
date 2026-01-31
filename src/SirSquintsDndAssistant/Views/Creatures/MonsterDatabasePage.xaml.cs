using SirSquintsDndAssistant.ViewModels.Creatures;

namespace SirSquintsDndAssistant.Views.Creatures;

public partial class MonsterDatabasePage : ContentPage
{
    private readonly MonsterDatabaseViewModel _viewModel;

    public MonsterDatabasePage(MonsterDatabaseViewModel viewModel)
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

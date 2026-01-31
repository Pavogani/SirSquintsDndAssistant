using SirSquintsDndAssistant.ViewModels.Reference;

namespace SirSquintsDndAssistant.Views.Reference;

public partial class ItemDatabasePage : ContentPage
{
    private readonly ItemDatabaseViewModel _viewModel;

    public ItemDatabasePage(ItemDatabaseViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadItemsCommand.ExecuteAsync(null);
    }
}

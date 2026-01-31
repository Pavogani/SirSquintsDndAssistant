using SirSquintsDndAssistant.ViewModels.BattleMap;
using SirSquintsDndAssistant.Controls;

namespace SirSquintsDndAssistant.Views.BattleMap;

public partial class BattleMapPage : ContentPage
{
    private readonly BattleMapViewModel _viewModel;

    public BattleMapPage(BattleMapViewModel viewModel)
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

    private void OnCellTapped(object? sender, MapCellEventArgs e)
    {
        _viewModel.HandleCellTap(e.GridX, e.GridY);
    }

    private void OnTokenSelected(object? sender, MapTokenEventArgs e)
    {
        _viewModel.SelectToken(e.Token);
    }

    private void OnTokenDragged(object? sender, MapTokenDragEventArgs e)
    {
        _viewModel.HandleTokenDrag(e.Token, e.NewGridX, e.NewGridY);
    }

    private async void OnFogChanged(object? sender, MapFogEventArgs e)
    {
        await _viewModel.SaveFogStateAsync(e.RevealedCells);
    }
}

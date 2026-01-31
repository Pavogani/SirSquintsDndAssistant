using SirSquintsDndAssistant.ViewModels.Reference;

namespace SirSquintsDndAssistant.Views.Reference;

public partial class SpellDetailPage : ContentPage
{
    public SpellDetailPage(SpellDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

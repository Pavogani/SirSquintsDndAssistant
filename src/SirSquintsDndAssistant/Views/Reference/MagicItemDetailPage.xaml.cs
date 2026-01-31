using SirSquintsDndAssistant.ViewModels.Reference;

namespace SirSquintsDndAssistant.Views.Reference;

public partial class MagicItemDetailPage : ContentPage
{
    public MagicItemDetailPage(MagicItemDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

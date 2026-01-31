using SirSquintsDndAssistant.ViewModels.Homebrew;

namespace SirSquintsDndAssistant.Views.Homebrew;

public partial class HomebrewItemEditPage : ContentPage
{
    public HomebrewItemEditPage(HomebrewItemEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

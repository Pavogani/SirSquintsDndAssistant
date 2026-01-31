using SirSquintsDndAssistant.ViewModels.Homebrew;

namespace SirSquintsDndAssistant.Views.Homebrew;

public partial class HomebrewSpellEditPage : ContentPage
{
    public HomebrewSpellEditPage(HomebrewSpellEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

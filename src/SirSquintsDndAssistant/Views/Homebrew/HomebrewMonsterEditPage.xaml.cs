using SirSquintsDndAssistant.ViewModels.Homebrew;

namespace SirSquintsDndAssistant.Views.Homebrew;

public partial class HomebrewMonsterEditPage : ContentPage
{
    public HomebrewMonsterEditPage(HomebrewMonsterEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

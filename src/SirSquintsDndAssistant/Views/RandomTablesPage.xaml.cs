using SirSquintsDndAssistant.ViewModels;

namespace SirSquintsDndAssistant.Views;

public partial class RandomTablesPage : ContentPage
{
    public RandomTablesPage(RandomTablesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

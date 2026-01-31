using SirSquintsDndAssistant.ViewModels.Utilities;

namespace SirSquintsDndAssistant.Views.Utilities;

public partial class AdvancedGeneratorsPage : ContentPage
{
    public AdvancedGeneratorsPage(AdvancedGeneratorsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

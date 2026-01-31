using SirSquintsDndAssistant.ViewModels.Reference;

namespace SirSquintsDndAssistant.Views.Reference;

public partial class RulesReferencePage : ContentPage
{
    public RulesReferencePage(RulesReferenceViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

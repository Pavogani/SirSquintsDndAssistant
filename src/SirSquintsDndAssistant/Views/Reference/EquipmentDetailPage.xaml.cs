using SirSquintsDndAssistant.ViewModels.Reference;

namespace SirSquintsDndAssistant.Views.Reference;

public partial class EquipmentDetailPage : ContentPage
{
    public EquipmentDetailPage(EquipmentDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

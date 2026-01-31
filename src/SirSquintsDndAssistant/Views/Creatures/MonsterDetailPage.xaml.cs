using SirSquintsDndAssistant.ViewModels.Creatures;

namespace SirSquintsDndAssistant.Views.Creatures;

public partial class MonsterDetailPage : ContentPage
{
    public MonsterDetailPage(MonsterDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

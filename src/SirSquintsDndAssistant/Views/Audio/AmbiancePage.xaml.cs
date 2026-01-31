using SirSquintsDndAssistant.ViewModels.Audio;

namespace SirSquintsDndAssistant.Views.Audio;

public partial class AmbiancePage : ContentPage
{
    public AmbiancePage(AmbianceViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private void OnAmbianceTabClicked(object? sender, EventArgs e)
    {
        AmbianceSection.IsVisible = true;
        SoundEffectsSection.IsVisible = false;
        AmbianceTabButton.BackgroundColor = Color.FromArgb("#512BD4"); // Primary color
        SoundEffectsTabButton.BackgroundColor = Colors.Gray;
    }

    private void OnSoundEffectsTabClicked(object? sender, EventArgs e)
    {
        AmbianceSection.IsVisible = false;
        SoundEffectsSection.IsVisible = true;
        AmbianceTabButton.BackgroundColor = Colors.Gray;
        SoundEffectsTabButton.BackgroundColor = Color.FromArgb("#512BD4"); // Primary color
    }
}

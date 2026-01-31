using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SirSquintsDndAssistant.Models.Creatures;
using SirSquintsDndAssistant.Services.Database.Repositories;
using SirSquintsDndAssistant.Services.Utilities;
using System.Collections.ObjectModel;

namespace SirSquintsDndAssistant.ViewModels.Creatures;

public partial class NpcListViewModel : BaseViewModel
{
    private readonly INpcRepository _npcRepository;
    private readonly IImageService _imageService;

    [ObservableProperty]
    private ObservableCollection<NPC> npcs = new();

    [ObservableProperty]
    private string newNpcName = string.Empty;

    [ObservableProperty]
    private string newNpcRace = string.Empty;

    [ObservableProperty]
    private string newNpcClass = string.Empty;

    [ObservableProperty]
    private string newNpcDescription = string.Empty;

    [ObservableProperty]
    private string? newNpcImagePath;

    public NpcListViewModel(INpcRepository npcRepository, IImageService imageService)
    {
        _npcRepository = npcRepository;
        _imageService = imageService;
        Title = "NPCs";
    }

    [RelayCommand]
    private async Task LoadNpcsAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            var npcs = await _npcRepository.GetAllAsync();
            Npcs.Clear();
            foreach (var npc in npcs)
                Npcs.Add(npc);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CreateNpcAsync()
    {
        if (string.IsNullOrWhiteSpace(NewNpcName))
            return;

        var npc = new NPC
        {
            Name = NewNpcName,
            Race = NewNpcRace,
            Class = NewNpcClass,
            Description = NewNpcDescription,
            ImagePath = NewNpcImagePath ?? string.Empty,
            Created = DateTime.Now,
            Modified = DateTime.Now
        };

        await _npcRepository.SaveAsync(npc);
        await LoadNpcsAsync();

        NewNpcName = string.Empty;
        NewNpcRace = string.Empty;
        NewNpcClass = string.Empty;
        NewNpcDescription = string.Empty;
        NewNpcImagePath = null;
    }

    [RelayCommand]
    private async Task DeleteNpcAsync(NPC npc)
    {
        // Delete associated image if exists
        if (!string.IsNullOrEmpty(npc.ImagePath))
        {
            await _imageService.DeleteImageAsync(npc.ImagePath);
        }

        await _npcRepository.DeleteAsync(npc);
        await LoadNpcsAsync();
    }

    [RelayCommand]
    private async Task PickImageAsync()
    {
        var imagePath = await _imageService.PickAndSaveImageAsync("npc");
        if (imagePath != null)
        {
            NewNpcImagePath = imagePath;
        }
    }

    [RelayCommand]
    private async Task ChangeNpcImageAsync(NPC npc)
    {
        var imagePath = await _imageService.PickAndSaveImageAsync($"npc_{npc.Id}");
        if (imagePath != null)
        {
            // Delete old image
            if (!string.IsNullOrEmpty(npc.ImagePath))
            {
                await _imageService.DeleteImageAsync(npc.ImagePath);
            }

            npc.ImagePath = imagePath;
            npc.Modified = DateTime.Now;
            await _npcRepository.SaveAsync(npc);
            await LoadNpcsAsync();
        }
    }

    [RelayCommand]
    private void ClearNewImage()
    {
        if (!string.IsNullOrEmpty(NewNpcImagePath))
        {
            _ = _imageService.DeleteImageAsync(NewNpcImagePath);
            NewNpcImagePath = null;
        }
    }
}

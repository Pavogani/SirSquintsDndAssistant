using CommunityToolkit.Maui;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace SirSquintsDndAssistant;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.UseSkiaSharp()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		// Register services (to be implemented)
		RegisterServices(builder.Services);

		return builder.Build();
	}

	private static void RegisterServices(IServiceCollection services)
	{
		// Database & Repositories
		services.AddSingleton<Services.Database.IDatabaseService, Services.Database.SqliteDatabaseService>();
		services.AddSingleton<Services.Database.Repositories.IMonsterRepository, Services.Database.Repositories.MonsterRepository>();
		services.AddSingleton<Services.Database.Repositories.ICampaignRepository, Services.Database.Repositories.CampaignRepository>();
		services.AddSingleton<Services.Database.Repositories.ISessionRepository, Services.Database.Repositories.SessionRepository>();
		services.AddSingleton<Services.Database.Repositories.IEncounterRepository, Services.Database.Repositories.EncounterRepository>();
		services.AddSingleton<Services.Database.Repositories.INpcRepository, Services.Database.Repositories.NpcRepository>();
		services.AddSingleton<Services.Database.Repositories.ISpellRepository, Services.Database.Repositories.SpellRepository>();
		services.AddSingleton<Services.Database.Repositories.ICombatRepository, Services.Database.Repositories.CombatRepository>();
		services.AddSingleton<Services.Database.Repositories.IQuestRepository, Services.Database.Repositories.QuestRepository>();
		services.AddSingleton<Services.Database.Repositories.IEquipmentRepository, Services.Database.Repositories.EquipmentRepository>();
		services.AddSingleton<Services.Database.Repositories.IMagicItemRepository, Services.Database.Repositories.MagicItemRepository>();
		services.AddSingleton<Services.Database.Repositories.IPlayerCharacterRepository, Services.Database.Repositories.PlayerCharacterRepository>();
		services.AddSingleton<Services.Database.Repositories.IConditionRepository, Services.Database.Repositories.ConditionRepository>();

		// API Clients
		services.AddHttpClient<Services.Api.IDnd5eApiClient, Services.Api.Dnd5eApiClient>();
		services.AddHttpClient<Services.Api.IOpen5eApiClient, Services.Api.Open5eApiClient>();

		// Business Services
		services.AddSingleton<Services.DataSync.IDataSyncService, Services.DataSync.DataSyncService>();
		services.AddSingleton<Services.DataSync.IDuplicateDetectionService, Services.DataSync.DuplicateDetectionService>();
		services.AddSingleton<Services.DataSync.IBackgroundSyncService, Services.DataSync.BackgroundSyncService>();
		services.AddSingleton<Services.Combat.ICombatService, Services.Combat.CombatService>();
		services.AddSingleton<Services.Combat.ICombatLogService, Services.Combat.CombatLogService>();
		services.AddSingleton<Services.Combat.ISpellSlotService, Services.Combat.SpellSlotService>();
		services.AddSingleton<Services.Combat.IStatusEffectService, Services.Combat.StatusEffectService>();
		services.AddSingleton<Services.Encounter.IDifficultyCalculator, Services.Encounter.DifficultyCalculator>();
		services.AddHttpClient<Services.Import.IDndBeyondImportService, Services.Import.DndBeyondImportService>();
		services.AddSingleton<Services.Import.IJsonDataImportService, Services.Import.JsonDataImportService>();
		services.AddSingleton<Services.Validation.IDataValidationService, Services.Validation.DataValidationService>();
		services.AddSingleton<Services.Export.IExportImportService, Services.Export.ExportImportService>();

		// Utilities (DialogService must be registered first as other services depend on it)
		services.AddSingleton<Services.Utilities.IDialogService, Services.Utilities.DialogService>();

		// Image Services
		services.AddHttpClient("ImageClient");
		services.AddSingleton<Services.Images.ICommunityImageService, Services.Images.CommunityImageService>();
		services.AddSingleton<Services.Utilities.DiceRoller>();
		services.AddSingleton<Services.Utilities.WeatherGenerator>();
		services.AddSingleton<Services.Utilities.NameGenerator>();
		services.AddSingleton<Services.Utilities.TreasureGenerator>();
		services.AddSingleton<Services.Utilities.IImageService, Services.Utilities.ImageService>();
		services.AddSingleton<Services.Utilities.IAdvancedGeneratorService, Services.Utilities.AdvancedGeneratorService>();
		services.AddSingleton<Services.Reference.IRulesReferenceService, Services.Reference.RulesReferenceService>();
		services.AddSingleton<Services.Utilities.IImageCacheService, Services.Utilities.ImageCacheService>();
		services.AddSingleton<Services.Utilities.IDebounceService, Services.Utilities.DebounceService>();
		services.AddSingleton<Services.Utilities.IMemoryManagementService, Services.Utilities.MemoryManagementService>();
		services.AddSingleton<Services.Utilities.IRandomTableService, Services.Utilities.RandomTableService>();
		services.AddSingleton<Services.Audio.IAudioService, Services.Audio.AudioService>();
		services.AddSingleton<Services.Homebrew.IHomebrewService, Services.Homebrew.HomebrewService>();
		services.AddSingleton<Services.SessionPrep.ISessionPrepService, Services.SessionPrep.SessionPrepService>();
		services.AddSingleton<Services.BattleMap.IBattleMapService, Services.BattleMap.BattleMapService>();
		services.AddSingleton<Services.Multiplayer.IPlayerSessionService, Services.Multiplayer.PlayerSessionService>();

		// ViewModels
		services.AddTransient<ViewModels.Creatures.MonsterDatabaseViewModel>();
		services.AddTransient<ViewModels.Creatures.MonsterDetailViewModel>();
		services.AddTransient<ViewModels.Creatures.NpcListViewModel>();
		services.AddTransient<ViewModels.Combat.InitiativeTrackerViewModel>();
		services.AddTransient<ViewModels.Encounter.EncounterBuilderViewModel>();
		services.AddTransient<ViewModels.Encounter.EncounterLibraryViewModel>();
		services.AddTransient<ViewModels.Campaign.CampaignListViewModel>();
		services.AddTransient<ViewModels.Campaign.SessionNotesViewModel>();
		services.AddTransient<ViewModels.Campaign.QuestTrackerViewModel>();
		services.AddTransient<ViewModels.Campaign.PlayerCharactersViewModel>();
		services.AddTransient<ViewModels.Reference.SpellbookViewModel>();
		services.AddTransient<ViewModels.Reference.SpellDetailViewModel>();
		services.AddTransient<ViewModels.Reference.ItemDatabaseViewModel>();
		services.AddTransient<ViewModels.Reference.EquipmentDetailViewModel>();
		services.AddTransient<ViewModels.Reference.MagicItemDetailViewModel>();
		services.AddTransient<ViewModels.Settings.SettingsViewModel>();
		services.AddTransient<ViewModels.Utilities.AdvancedGeneratorsViewModel>();
		services.AddTransient<ViewModels.Reference.RulesReferenceViewModel>();
		services.AddTransient<ViewModels.RandomTablesViewModel>();
		services.AddTransient<ViewModels.Audio.AmbianceViewModel>();
		services.AddTransient<ViewModels.Homebrew.HomebrewViewModel>();
		services.AddTransient<ViewModels.Homebrew.HomebrewMonsterEditViewModel>();
		services.AddTransient<ViewModels.Homebrew.HomebrewSpellEditViewModel>();
		services.AddTransient<ViewModels.Homebrew.HomebrewItemEditViewModel>();
		services.AddTransient<ViewModels.BattleMap.BattleMapViewModel>();
		services.AddTransient<ViewModels.Campaign.SessionPrepViewModel>();
		services.AddTransient<ViewModels.Multiplayer.PlayerSessionViewModel>();

		// Views
		services.AddTransient<Views.Settings.DataSyncPage>();
		services.AddTransient<Views.Creatures.MonsterDatabasePage>();
		services.AddTransient<Views.Creatures.MonsterDetailPage>();
		services.AddTransient<Views.Creatures.NpcListPage>();
		services.AddTransient<Views.Combat.InitiativeTrackerPage>();
		services.AddTransient<Views.Encounter.EncounterBuilderPage>();
		services.AddTransient<Views.Encounter.EncounterLibraryPage>();
		services.AddTransient<Views.Campaign.CampaignListPage>();
		services.AddTransient<Views.Campaign.SessionNotesPage>();
		services.AddTransient<Views.Campaign.QuestTrackerPage>();
		services.AddTransient<Views.Campaign.PlayerCharactersPage>();
		services.AddTransient<Views.Reference.SpellbookPage>();
		services.AddTransient<Views.Reference.SpellDetailPage>();
		services.AddTransient<Views.Reference.ItemDatabasePage>();
		services.AddTransient<Views.Reference.EquipmentDetailPage>();
		services.AddTransient<Views.Reference.MagicItemDetailPage>();
		services.AddTransient<Views.Settings.SettingsPage>();
		services.AddTransient<Views.Utilities.AdvancedGeneratorsPage>();
		services.AddTransient<Views.Reference.RulesReferencePage>();
		services.AddTransient<Views.RandomTablesPage>();
		services.AddTransient<Views.Audio.AmbiancePage>();
		services.AddTransient<Views.Homebrew.HomebrewPage>();
		services.AddTransient<Views.Homebrew.HomebrewMonsterEditPage>();
		services.AddTransient<Views.Homebrew.HomebrewSpellEditPage>();
		services.AddTransient<Views.Homebrew.HomebrewItemEditPage>();
		services.AddTransient<Views.BattleMap.BattleMapPage>();
		services.AddTransient<Views.Campaign.SessionPrepPage>();
		services.AddTransient<Views.Multiplayer.PlayerSessionPage>();
	}
}

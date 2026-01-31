using SirSquintsDndAssistant.Views.Creatures;
using SirSquintsDndAssistant.Views.Reference;
using SirSquintsDndAssistant.Views.Homebrew;

namespace SirSquintsDndAssistant;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// Register routes for navigation
		Routing.RegisterRoute("monsterdetail", typeof(MonsterDetailPage));
		Routing.RegisterRoute("spelldetail", typeof(SpellDetailPage));
		Routing.RegisterRoute("equipmentdetail", typeof(EquipmentDetailPage));
		Routing.RegisterRoute("magicitemdetail", typeof(MagicItemDetailPage));

		// Homebrew edit routes
		Routing.RegisterRoute("homebrewMonsterEdit", typeof(HomebrewMonsterEditPage));
		Routing.RegisterRoute("homebrewSpellEdit", typeof(HomebrewSpellEditPage));
		Routing.RegisterRoute("homebrewItemEdit", typeof(HomebrewItemEditPage));
	}
}

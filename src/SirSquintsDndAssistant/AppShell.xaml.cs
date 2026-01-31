using SirSquintsDndAssistant.Views.Creatures;
using SirSquintsDndAssistant.Views.Reference;

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
	}
}

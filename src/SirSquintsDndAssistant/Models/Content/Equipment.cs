using SQLite;

namespace SirSquintsDndAssistant.Models.Content;

public class Equipment
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string ApiId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string EquipmentCategory { get; set; } = string.Empty;
    public string WeaponCategory { get; set; } = string.Empty;
    public string WeaponRange { get; set; } = string.Empty;
    public string ArmorCategory { get; set; } = string.Empty;
    public int Cost { get; set; }
    public string CostCurrency { get; set; } = string.Empty;
    public double Weight { get; set; }
    public string DescriptionJson { get; set; } = string.Empty;
    public string PropertiesJson { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty; // Local cached/custom image
    public string Source { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
}

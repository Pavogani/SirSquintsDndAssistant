using SirSquintsDndAssistant.Models.Campaign;
using SirSquintsDndAssistant.Services.Database;

namespace SirSquintsDndAssistant.Services.SessionPrep;

public interface ISessionPrepService
{
    // Session Prep Items
    Task<List<SessionPrepItem>> GetPrepItemsForSessionAsync(int sessionId);
    Task<List<SessionPrepItem>> GetPrepItemsForCampaignAsync(int campaignId);
    Task<SessionPrepItem> SavePrepItemAsync(SessionPrepItem item);
    Task DeletePrepItemAsync(int id);
    Task MarkItemCompletedAsync(int id, bool completed);
    Task ReorderPrepItemsAsync(List<int> orderedIds);

    // Wiki Entries
    Task<List<WikiEntry>> GetWikiEntriesForCampaignAsync(int campaignId);
    Task<List<WikiEntry>> GetWikiEntriesByCategoryAsync(int campaignId, WikiCategory category);
    Task<WikiEntry?> GetWikiEntryAsync(int id);
    Task<List<WikiEntry>> SearchWikiAsync(int campaignId, string searchText);
    Task<WikiEntry> SaveWikiEntryAsync(WikiEntry entry);
    Task DeleteWikiEntryAsync(int id);

    // Quick Reference
    Task<List<SessionPrepItem>> GetUpcomingEncountersAsync(int campaignId);
    Task<List<WikiEntry>> GetKeyNpcsAsync(int campaignId);
    Task<List<WikiEntry>> GetActiveLocationsAsync(int campaignId);
}

public class SessionPrepService : ISessionPrepService
{
    private readonly IDatabaseService _databaseService;

    public SessionPrepService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    #region Session Prep Items

    public async Task<List<SessionPrepItem>> GetPrepItemsForSessionAsync(int sessionId)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<SessionPrepItem>()
                .Where(p => p.SessionId == sessionId)
                .OrderBy(p => p.SortOrder)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting session prep items: {ex.Message}");
            return new List<SessionPrepItem>();
        }
    }

    public async Task<List<SessionPrepItem>> GetPrepItemsForCampaignAsync(int campaignId)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<SessionPrepItem>()
                .Where(p => p.CampaignId == campaignId)
                .OrderBy(p => p.SortOrder)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting campaign prep items: {ex.Message}");
            return new List<SessionPrepItem>();
        }
    }

    public async Task<SessionPrepItem> SavePrepItemAsync(SessionPrepItem item)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            item.UpdatedAt = DateTime.Now;

            if (item.Id == 0)
            {
                item.CreatedAt = DateTime.Now;
                // Get the next sort order
                var maxOrder = await db.Table<SessionPrepItem>()
                    .Where(p => p.SessionId == item.SessionId)
                    .OrderByDescending(p => p.SortOrder)
                    .FirstOrDefaultAsync();
                item.SortOrder = (maxOrder?.SortOrder ?? 0) + 1;

                await db.InsertAsync(item);
            }
            else
            {
                await db.UpdateAsync(item);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving prep item: {ex.Message}");
        }

        return item;
    }

    public async Task DeletePrepItemAsync(int id)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            await db.DeleteAsync<SessionPrepItem>(id);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting prep item: {ex.Message}");
        }
    }

    public async Task MarkItemCompletedAsync(int id, bool completed)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            var item = await db.GetAsync<SessionPrepItem>(id);
            if (item != null)
            {
                item.IsCompleted = completed;
                item.UpdatedAt = DateTime.Now;
                await db.UpdateAsync(item);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error marking prep item completed: {ex.Message}");
        }
    }

    public async Task ReorderPrepItemsAsync(List<int> orderedIds)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            for (int i = 0; i < orderedIds.Count; i++)
            {
                var item = await db.GetAsync<SessionPrepItem>(orderedIds[i]);
                if (item != null)
                {
                    item.SortOrder = i;
                    await db.UpdateAsync(item);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error reordering prep items: {ex.Message}");
        }
    }

    #endregion

    #region Wiki Entries

    public async Task<List<WikiEntry>> GetWikiEntriesForCampaignAsync(int campaignId)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<WikiEntry>()
                .Where(w => w.CampaignId == campaignId)
                .OrderBy(w => w.Category)
                .ThenBy(w => w.Title)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting wiki entries: {ex.Message}");
            return new List<WikiEntry>();
        }
    }

    public async Task<List<WikiEntry>> GetWikiEntriesByCategoryAsync(int campaignId, WikiCategory category)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<WikiEntry>()
                .Where(w => w.CampaignId == campaignId && w.Category == category)
                .OrderBy(w => w.Title)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting wiki entries by category: {ex.Message}");
            return new List<WikiEntry>();
        }
    }

    public async Task<WikiEntry?> GetWikiEntryAsync(int id)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.GetAsync<WikiEntry>(id);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting wiki entry: {ex.Message}");
            return null;
        }
    }

    public async Task<List<WikiEntry>> SearchWikiAsync(int campaignId, string searchText)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            var search = searchText.ToLower();
            return await db.Table<WikiEntry>()
                .Where(w => w.CampaignId == campaignId &&
                           (w.Title.ToLower().Contains(search) ||
                            w.Content.ToLower().Contains(search)))
                .OrderBy(w => w.Title)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error searching wiki: {ex.Message}");
            return new List<WikiEntry>();
        }
    }

    public async Task<WikiEntry> SaveWikiEntryAsync(WikiEntry entry)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            entry.UpdatedAt = DateTime.Now;

            if (entry.Id == 0)
            {
                entry.CreatedAt = DateTime.Now;
                await db.InsertAsync(entry);
            }
            else
            {
                await db.UpdateAsync(entry);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving wiki entry: {ex.Message}");
        }

        return entry;
    }

    public async Task DeleteWikiEntryAsync(int id)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            await db.DeleteAsync<WikiEntry>(id);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting wiki entry: {ex.Message}");
        }
    }

    #endregion

    #region Quick Reference

    public async Task<List<SessionPrepItem>> GetUpcomingEncountersAsync(int campaignId)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<SessionPrepItem>()
                .Where(p => p.CampaignId == campaignId &&
                           p.ItemType == PrepItemType.Encounter &&
                           !p.IsCompleted)
                .OrderBy(p => p.SortOrder)
                .Take(5)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting upcoming encounters: {ex.Message}");
            return new List<SessionPrepItem>();
        }
    }

    public async Task<List<WikiEntry>> GetKeyNpcsAsync(int campaignId)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<WikiEntry>()
                .Where(w => w.CampaignId == campaignId &&
                           w.Category == WikiCategory.Character &&
                           w.IsPlayerKnown)
                .OrderBy(w => w.Title)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting key NPCs: {ex.Message}");
            return new List<WikiEntry>();
        }
    }

    public async Task<List<WikiEntry>> GetActiveLocationsAsync(int campaignId)
    {
        try
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<WikiEntry>()
                .Where(w => w.CampaignId == campaignId &&
                           w.Category == WikiCategory.Location &&
                           w.IsPlayerKnown)
                .OrderBy(w => w.Title)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting active locations: {ex.Message}");
            return new List<WikiEntry>();
        }
    }

    #endregion
}

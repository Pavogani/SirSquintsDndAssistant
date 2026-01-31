namespace SirSquintsDndAssistant.Services.Utilities;

public interface IImageService
{
    Task<string?> PickAndSaveImageAsync(string prefix);
    Task<bool> DeleteImageAsync(string imagePath);
    string GetPlaceholderImagePath(string type);
}

public class ImageService : IImageService
{
    private readonly string _imagesFolder;
    private readonly IDialogService _dialogService;

    public ImageService(IDialogService dialogService)
    {
        _dialogService = dialogService;
        _imagesFolder = Path.Combine(FileSystem.AppDataDirectory, "images");

        try
        {
            if (!Directory.Exists(_imagesFolder))
            {
                Directory.CreateDirectory(_imagesFolder);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating images folder: {ex.Message}");
        }
    }

    public async Task<string?> PickAndSaveImageAsync(string prefix)
    {
        try
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var result = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Select an image"
            });
#pragma warning restore CS0618

            if (result == null)
                return null;

            // Generate unique filename
            var extension = Path.GetExtension(result.FileName);
            var newFileName = $"{prefix}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
            var destinationPath = Path.Combine(_imagesFolder, newFileName);

            // Copy the file to our images folder
            using var sourceStream = await result.OpenReadAsync();
            using var destinationStream = File.Create(destinationPath);
            await sourceStream.CopyToAsync(destinationStream);

            return destinationPath;
        }
        catch (PermissionException)
        {
            await _dialogService.DisplayAlertAsync(
                "Permission Denied",
                "Photo library access is required to select images.");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error picking image: {ex.Message}");
            return null;
        }
    }

    public Task<bool> DeleteImageAsync(string imagePath)
    {
        try
        {
            if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
            {
                File.Delete(imagePath);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting image: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    public string GetPlaceholderImagePath(string type)
    {
        // Return a resource path for placeholder images
        // These would be embedded resources in the app
        return type.ToLower() switch
        {
            "npc" => "npc_placeholder.png",
            "monster" => "monster_placeholder.png",
            "player" => "player_placeholder.png",
            _ => "default_placeholder.png"
        };
    }
}

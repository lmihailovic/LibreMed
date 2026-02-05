using System.Threading.Tasks;

namespace LibreMed.Services;

public interface IFileSaveDialogService
{
    Task<string?> PickPdfSavePathAsync(string suggestedFileName);

    Task<string?> PickJsonSavePathAsync(string suggestedFileName);
    Task<string?> PickJsonOpenPathAsync();
}
using System.Threading.Tasks;

namespace LibreMed.Services;

public interface IFileSaveDialogService
{
    Task<string?> PickPdfSavePathAsync(string suggestedFileName);
}
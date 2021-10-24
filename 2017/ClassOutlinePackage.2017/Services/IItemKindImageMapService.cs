namespace ClassOutline.Services
{
    public interface IItemKindImageMapService
    {
        string getImageKey(string kind);
        string DefaultKey { get; }
    }
}
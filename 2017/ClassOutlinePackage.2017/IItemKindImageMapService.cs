namespace ClassOutline
{
    public interface IItemKindImageMapService
    {
        string getImageKey(string kind);
        string DefaultKey { get; }
    }
}
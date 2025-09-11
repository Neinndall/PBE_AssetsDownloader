namespace PBE_AssetsManager.Views.Models
{
    public class VersionFileInfo
    {
        public string FileName { get; set; }
        public string Content { get; set; }
        public string Category { get; set; } // To group files like 'league-client', 'lol-game-client-sln'
    }
}

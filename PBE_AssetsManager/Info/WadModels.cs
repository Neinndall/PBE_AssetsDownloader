using LeagueToolkit.Core.Wad;

namespace PBE_AssetsManager.Info
{
    public enum ChunkDiffType
    {
        New,
        Removed,
        Modified,
        Renamed
    }

    public class ChunkDiff
    {
        public ChunkDiffType Type { get; set; }
        public WadChunk OldChunk { get; set; }
        public WadChunk NewChunk { get; set; }
        public string OldPath { get; set; }
        public string NewPath { get; set; }
        public string SourceWadFile { get; set; }
    }
}

using System.Collections.Generic;
using LeagueToolkit.Core.Wad;
using PBE_AssetsManager.Views.Dialogs;

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

    public class SerializableComparisonResult
    {
        public string OldPbePath { get; set; }
        public string NewPbePath { get; set; }
        public List<SerializableChunkDiff> Diffs { get; set; }
    }
}

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace lyricism.Extractors
{
    public class FileCacheExtractor : LyricExtractor
    {
        public FileCacheExtractor(string artistName, string trackName) : base(artistName, trackName)
        {
            this.Order = 0;
            this.IsCache = true;
            this.SourceName = "FileCache";
        }
        public override void GetLyrics()
        {
            if (!System.IO.Directory.Exists(Program.LyricsCacheDir))
            {
                this.DebugLog.Add("Cache folder is missing.");
                return;
            }

            var matchingPaths = Program.LyricsCacheDir.GetDirectories()
                .Where(d => d.Sanitize().SearchTermMatch(this.SearchArtistName.Sanitize()))
                .SelectMany(d => d.GetDirectories())
                .Where(d => d.Sanitize().SearchTermMatch(this.SearchTrackName.Sanitize()))
                .SelectMany(d => d.GetFiles())
                .ToArray();

            if (!matchingPaths.Any())
            {
                this.DebugLog.Add("No match found.");
                CheckedLyrics = true;
                return;
            }

            foreach (var path in matchingPaths)
            {
                var lyrics = System.IO.File.ReadAllText(path);

                if (lyrics == null || lyrics.ContainsAny(LyricErrors))
                {
                    this.DebugLog.Add("No lyrics found.");
                    continue;
                }

                var pathElements = path.Split(System.IO.Path.DirectorySeparatorChar).Reverse().ToArray();

                SubSourceName = System.IO.Path.GetFileNameWithoutExtension(pathElements[0]);
                TrackName = pathElements[1];
                ArtistName = pathElements[2];
                Lyrics = lyrics;
                break;
            }

            CheckedLyrics = true;
        }
    }
}

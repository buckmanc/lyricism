using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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
                return;
            }

            var matchingPath = Program.LyricsCacheDir.GetDirectories()
                .Where(d => d.Sanitize().Contains(this.SearchArtistName.Sanitize(), StringComparison.InvariantCultureIgnoreCase))
                .SelectMany(d => d.GetDirectories())
                .Where(d => d.Sanitize().Contains(this.SearchTrackName.Sanitize(), StringComparison.InvariantCultureIgnoreCase))
                .SelectMany(d => d.GetFiles())
                .FirstOrDefault();

            if (String.IsNullOrWhiteSpace(matchingPath))
            {
                // Console.WriteLine("FileCache error: could not find matching path");
                CheckedLyrics = true;
                return;
            }

            var pathElements = matchingPath.Split(System.IO.Path.DirectorySeparatorChar).Reverse().ToArray();

            SubSourceName = System.IO.Path.GetFileNameWithoutExtension(pathElements[0]);
            TrackName = pathElements[1];
            ArtistName = pathElements[2];
            Lyrics = System.IO.File.ReadAllText(matchingPath);
            CheckedLyrics = true;
        }
    }
}

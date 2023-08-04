using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lyricism.Extractors
{
    public class MetallumExtractor : LyricExtractor
    {

        private const string SearchURL = "https://www.metal-archives.com/search/ajax-advanced/searching/songs?";
        private const string LyricsURL = "https://www.metal-archives.com/release/ajax-view-lyrics/id/";


        public MetallumExtractor(string artistName, string trackName, string? albumName = null) : base(artistName, trackName, albumName)
        {
            this.SourceName = "Encylopaedia Metallum";
        }

        public override void GetLyrics()
        {
            var search = HttpClient.GetPageSource(SearchURL + "bandName=" + SearchArtistName + "&songTitle=" + SearchTrackName);
            var parsedJson = JObject.Parse(search);
            if (!parsedJson.TryGetValue("aaData", out var aaData))
            {
                this.CheckedLyrics = true;
                return;
            }

            var items = JsonConvert.DeserializeObject<List<List<string>>>(aaData.ToString()) ?? new();

            foreach (var item in items)
            {
                var lyricID = item[4].RegexMatch(@"id=.+[a-z]+.(?<id>\d+)", "id");
                var lyrics = HttpClient.GetPageSource(LyricsURL + lyricID);
                if (lyrics.Contains("(lyrics not available)"))
                    continue;

                ArtistName = item[0].RegexMatch(@"title=.+>(?<name>.*)<", "name");
                TrackName = item[3];
                Lyrics = lyrics.StripHTML().Trim();

                break;
            }
            this.CheckedLyrics = true;
        }
    }
}

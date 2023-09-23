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


        public MetallumExtractor(string artistName, string trackName) : base(artistName, trackName)
        {
            this.Order = 80;
            this.SourceName = "Encylopaedia Metallum";
        }

        public override void GetLyrics()
        {
            var search = HttpClient.GetPageSource(SearchURL
                + "bandName=" + SearchArtistName.UrlEncode()
                + "&songTitle=" + SearchTrackName.UrlEncode()
                );
            var parsedJson = JObject.Parse(search);
            if (!parsedJson.TryGetValue("aaData", out var aaData))
            {
                this.DebugLog.Add("No search results.");
                this.CheckedLyrics = true;
                return;
            }

            var items = JsonConvert.DeserializeObject<List<List<string>>>(aaData.ToString()) ?? new();

            foreach (var item in items)
            {
                var artistName = item[0].RegexMatch(@"title=.+>(?<name>.*)<", "name");
                var trackName = item[3];

                this.DebugLog.Add("Artist: " + artistName);
                this.DebugLog.Add("Track: " + trackName);

                if (!artistName.SearchTermMatch(this.SearchArtistName) || !trackName.SearchTermMatch(this.SearchTrackName))
                    continue;

                var lyricID = item[4].RegexMatch(@"id=.+[a-z]+.(?<id>\d+)", "id");
                var lyrics = HttpClient.GetPageSource(LyricsURL + lyricID);
                if (lyrics.IsNullOrWhiteSpace() || lyrics.ContainsAny(LyricErrors))
                {
                    this.DebugLog.Add("No lyrics found.");
                    continue;
                }

                ArtistName = artistName;
                TrackName = trackName;
                Lyrics = lyrics;

                break;
            }
            this.CheckedLyrics = true;
        }
    }
}

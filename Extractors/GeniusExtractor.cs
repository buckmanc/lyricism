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
    public class GeniusExtractor : LyricExtractor
    {
        private const string SearchURL = "https://genius.com/api/search/multi?q=";


        public GeniusExtractor(string artistName, string trackName, string? albumName = null) : base(artistName, trackName, albumName)
        {
            this.Order = 10;
            this.SourceName = "Genius";
        }
        public override void GetLyrics()
        {
            // TODO should probably URL encode get parameters
            var url = SearchURL + (this.SearchArtistName + " " + this.SearchTrackName).Replace(" ", "+");

            var search = HttpClient.GetPageSource(url);
            var parsedJson = JObject.Parse(search);
            IEnumerable<JToken> jsonTokens = parsedJson.SelectTokens("response.sections[?(@.type == 'song')].hits[*].result");
            if (jsonTokens == null)
            {
                this.CheckedLyrics = true;
                return;
            }

            foreach (var jsonToken in jsonTokens)
            {
                var lyricsURL = "https://genius.com" + jsonToken["path"];
                var lyrics = HttpClient.GetPageSource(lyricsURL);
                lyrics = lyrics
                    .RegexMatches(@"data-lyrics-container.+?>(?<value>.+?)</div>", "value")
                    .Join(Environment.NewLine)
                    .Replace("<br/>", Environment.NewLine)
                    ;
                lyrics = System.Web.HttpUtility.HtmlDecode(lyrics);
                // deserialize directly to string to handle escape sequences
                // lyrics = JsonConvert.DeserializeObject<string>(lyrics);
                // lyrics = Regex.Unescape(lyrics);
                if (lyrics == null || lyrics.Contains("(lyrics not available)"))
                    continue;

                ArtistName = jsonToken["artist_names"].ToString();
                TrackName = jsonToken["title"].ToString();
                Lyrics = lyrics.StripHTML().Trim();

                break;
            }

            this.CheckedLyrics = true;
        }
    }
}

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
    public class AZLyricsExtractor : LyricExtractor
    {
        private const string SearchURL = "https://search.azlyrics.com/suggest.php?q=";


        public AZLyricsExtractor(string artistName, string trackName) : base(artistName, trackName)
        {
            this.Order = 20;
            this.SourceName = "AZLyrics";
        }

        public override void GetLyrics()
        {
            var url = SearchURL + (this.SearchArtistName + " " + this.SearchTrackName).UrlEncode();

            var search = HttpClient.GetPageSource(url);
            var parsedJson = JObject.Parse(search);
            IEnumerable<JToken> jsonTokens = parsedJson.SelectToken("songs");
            if (jsonTokens == null)
            {
                this.DebugLog.Add("No search results.");
                this.CheckedLyrics = true;
                return;
            }

            foreach (var jsonToken in jsonTokens)
            {
                var lyricsURL = Regex.Unescape(jsonToken["url"].ToString());

                var pageSource = HttpClient.GetPageSource(lyricsURL);
                var artistName = pageSource.RegexMatch("ArtistName = \"(?<value>.+?)\";", "value");
                var trackName = pageSource.RegexMatch("SongName = \"(?<value>.+?)\";", "value");

                this.DebugLog.Add("Artist: " + artistName);
                this.DebugLog.Add("Track: " + trackName);

                if (!artistName.SearchTermMatch(this.SearchArtistName) || !trackName.SearchTermMatch(this.SearchTrackName))
                    continue;

                var lyrics = pageSource
                    .RegexMatches(@"<!-.*?Usage of azlyrics.+?-->(?<value>.+?)</div>", "value")
                    .Join("\n")
                    .Replace("<br/>", "\n")
                    .Trim()
                    ;
                lyrics = System.Web.HttpUtility.HtmlDecode(lyrics);
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

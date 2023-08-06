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


        public AZLyricsExtractor(string artistName, string trackName, string? albumName = null) : base(artistName, trackName, albumName)
        {
            this.Order = 20;
            this.SourceName = "AZLyrics";
        }
        public override void GetLyrics()
        {
            // TODO should probably URL encode get parameters
            var url = SearchURL + System.Web.HttpUtility.UrlPathEncode(this.SearchArtistName + " " + this.SearchTrackName);

            var search = HttpClient.GetPageSource(url);
            var parsedJson = JObject.Parse(search);
            IEnumerable<JToken> jsonTokens = parsedJson.SelectToken("songs");
            if (jsonTokens == null)
            {
                this.CheckedLyrics = true;
                return;
            }

            foreach (var jsonToken in jsonTokens)
            {
                var lyricsURL = Regex.Unescape(jsonToken["url"].ToString());
                var pageSource = HttpClient.GetPageSource(lyricsURL);
                var lyrics = pageSource
                    .RegexMatches(@"<!-.*?Usage of azlyrics.+?-->(?<value>.+?)</div>", "value")
                    .Join(Environment.NewLine)
                    .Replace("<br/>", Environment.NewLine)
                    .Trim()
                    ;
                lyrics = System.Web.HttpUtility.HtmlDecode(lyrics);
                if (lyrics == null || lyrics.Contains("(lyrics not available)"))
                    continue;
                ArtistName = pageSource.RegexMatch("ArtistName = \"(?<value>.+?)\";", "value");
                TrackName = pageSource.RegexMatch("SongName = \"(?<value>.+?)\";", "value");
                

                Lyrics = lyrics.StripHTML().Trim();

                break;
            }

            this.CheckedLyrics = true;
        }
    }
}

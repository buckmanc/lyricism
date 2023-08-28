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


        public GeniusExtractor(string artistName, string trackName) : base(artistName, trackName)
        {
            this.Order = 10;
            this.SourceName = "Genius";
        }
        public override void GetLyrics()
        {
            var url = SearchURL + System.Web.HttpUtility.UrlEncode(this.SearchArtistName + " " + this.SearchTrackName);

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
                // HtmlDecode to string to handle escape sequences
                lyrics = System.Web.HttpUtility.HtmlDecode(lyrics);
                if (lyrics == null || lyrics.Contains("(lyrics not available)"))
                    continue;

                lyrics = lyrics.StripHTML().Trim();

                // genius is inserting the cyrillic ye into otherwise duplicate lines
                var yeTest = lyrics.RegexMatches(@"\p{IsCyrillic}").SelectMany(s => s).ToArray();
                if (yeTest.Any() && yeTest.Distinct().Count() == 1)
                {
                    if (yeTest.First() == 'е')
                    lyrics = lyrics.Replace('е', 'e');
                    else
                        this.DebugLog.Add("Solitary Cyrillic character detected: " + yeTest.First());
                }


                ArtistName = jsonToken["artist_names"].ToString();
                TrackName = jsonToken["title"].ToString();
                Lyrics = lyrics;

                break;
            }

            this.CheckedLyrics = true;
        }
    }
}

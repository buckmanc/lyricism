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
            var url = SearchURL + (this.SearchArtistName + " " + this.SearchTrackName).UrlEncode();

            var search = HttpClient.GetPageSource(url);
            var parsedJson = JObject.Parse(search);
            IEnumerable<JToken> jsonTokens = parsedJson.SelectTokens("response.sections[?(@.type == 'song')].hits[*].result");
            if (jsonTokens == null)
            {
                this.DebugLog.Add("No search results.");
                this.CheckedLyrics = true;
                return;
            }

            foreach (var jsonToken in jsonTokens)
            {
                var artistName = jsonToken["artist_names"].ToString();
                var trackName = jsonToken["title"].ToString();

                this.DebugLog.Add("Artist: " + artistName);
                this.DebugLog.Add("Track: " + trackName);

                if (!artistName.SearchTermMatch(this.SearchArtistName) || !trackName.SearchTermMatch(this.SearchTrackName))
                    continue;

                var lyricsURL = "https://genius.com" + jsonToken["path"];
                var lyrics = HttpClient.GetPageSource(lyricsURL);
                lyrics = lyrics
                    .RegexMatches(@"data-lyrics-container.+?>(?<value>.+?)</div>", "value")
                    .Join("\n")
                    .Replace("<br/>", "\n")
                    ;

                // HtmlDecode to string to handle escape sequences
                lyrics = System.Web.HttpUtility.HtmlDecode(lyrics);
                if (lyrics.IsNullOrWhiteSpace() || lyrics.ContainsAny(LyricErrors))
                {
                    this.DebugLog.Add("No lyrics found.");
                    continue;
                }

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


                ArtistName = artistName;
                TrackName = trackName;
                Lyrics = lyrics;

                break;
            }

            this.CheckedLyrics = true;
        }
    }
}

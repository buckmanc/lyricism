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
    public class LetrasDotComExtractor : LyricExtractor
    {
        private const string SearchURL = "https://solr.sscdn.co/letras/m1/?wt=json&callback=LetrasSug&q=";


        public LetrasDotComExtractor(string artistName, string trackName) : base(artistName, trackName)
        {
            this.Order = 40;
            this.SourceName = "Letras.com";
        }
        public override void GetLyrics()
        {
            var url = SearchURL + (this.SearchArtistName + " " + this.SearchTrackName).UrlEncode();

            var search = HttpClient.GetPageSource(url);
            search = search.Trim().TrimStart("LetrasSug(").TrimEnd(")");
            // Console.WriteLine("url: " +url);
            // Console.WriteLine("search: " + search);
            var parsedJson = JObject.Parse(search);
            IEnumerable<JToken> jsonTokens = parsedJson.SelectTokens("response.docs[*]");
            if (jsonTokens == null)
            {
                this.DebugLog.Add("No search results.");
                this.CheckedLyrics = true;
                return;
            }

            foreach (var jsonToken in jsonTokens)
            {
                var artistName = jsonToken["art"].ToString();
                var trackName = jsonToken["txt"].ToString();

                this.DebugLog.Add("Artist: " + artistName);
                this.DebugLog.Add("Track: " + trackName);

                if (!artistName.SearchTermMatch(this.SearchArtistName) || !trackName.SearchTermMatch(this.SearchTrackName))
                    continue;

                var lyricsURL = "https://letras.com/" + jsonToken["dns"] + "/" + jsonToken["url"];
                var lyrics = HttpClient.GetPageSource(lyricsURL);

                System.IO.File.WriteAllText("test.txt", lyrics);

                var newLineTags = new string[]{"<br/>", "<p>", "</p>"};

                lyrics = lyrics
                    .RegexMatches(@"""lyric-original""> (?<value>.+?)</div>", "value")
                    .Join("\n")
                    .Replace(newLineTags, "\n")
                    ;
                // HtmlDecode to string to handle escape sequences
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

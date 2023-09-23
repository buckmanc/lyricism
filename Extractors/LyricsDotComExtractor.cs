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
    public class LyricsDotComExtractor : LyricExtractor
    {
        private const string SearchURL = "https://www.lyrics.com/gw.php";


        public LyricsDotComExtractor(string artistName, string trackName) : base(artistName, trackName)
        {
            this.Order = 30;
            this.SourceName = "Lyrics.com";
        }
        public override void GetLyrics()
        {
            // can only search by artist
            var postData = new Dictionary<string, string>();
            postData.Add("action", "get_ac");
            postData.Add("term", this.SearchArtistName);
            postData.Add("type", "1");

            var search = HttpClient.GetPageSource(SearchURL, postData, Program.PostDataType.Form);
            var jsonTokens = JArray.Parse(search)
                ?.Where(x => x != null && x.HasValues)
                ?.ToArray();
            if (jsonTokens == null)
            {
                this.DebugLog.Add("No search results.");
                this.CheckedLyrics = true;
                return;
            }

            // Console.WriteLine(search);

            foreach (var jsonToken in jsonTokens)
            {
                var artistName = jsonToken["value"].ToString();
                this.DebugLog.Add("Artist: " + artistName);

                if (jsonToken["category"].ToString() != "Artists" || !artistName.SearchTermMatch(SearchArtistName))
                    continue;

                var artistURL = Regex.Unescape(jsonToken["link"].ToString());
                var artistPageSource = HttpClient.GetPageSource(artistURL);
                var trackLinks = artistPageSource.RegexMatchesGroups(@"""tal qx"".+?href=""(?<url>.+?)"">(?<trackName>.+?)<")
                    .Select(g => new {
                        URL = "https://www.lyrics.com" +  g["url"].Value,
                        TrackName = g["trackName"].Value,
                    })
                    .Where(x => x.TrackName.SearchTermMatch(SearchTrackName))
                    .ToArray();

                if (!trackLinks.Any())
                {
                    this.DebugLog.Add("No tracks found.");
                    continue;
                }

                foreach (var trackLink in trackLinks)
                {
                    this.DebugLog.Add("Track: " + trackLink.TrackName);

                    var trackPageSource = HttpClient.GetPageSource(trackLink.URL);
                    // lyric-body
                    var lyrics = trackPageSource
                        .RegexMatches(@"""lyric-body"".+?>(?<value>.+?)</pre>", "value")
                        .Join("\n")
                        .Replace("<br/>", "\n")
                        .Trim()
                        ;
                    // lyrics = System.Web.HttpUtility.HtmlDecode(lyrics);
                    if (lyrics.IsNullOrWhiteSpace() || lyrics.ContainsAny(LyricErrors))
                    {
                        this.DebugLog.Add("No lyrics found.");
                        continue;
                    }

                    ArtistName = artistName;
                    TrackName = trackLink.TrackName;
                    Lyrics = lyrics;

                    break;
                }
            }

            this.CheckedLyrics = true;
        }
    }
}

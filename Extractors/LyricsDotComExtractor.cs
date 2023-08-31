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
                this.CheckedLyrics = true;
                return;
            }

            // Console.WriteLine(search);

            foreach (var jsonToken in jsonTokens)
            {
                if (jsonToken["category"].ToString() != "Artists")
                    continue;

                var artistURL = Regex.Unescape(jsonToken["link"].ToString());
                var artistPageSource = HttpClient.GetPageSource(artistURL);
                var trackLinks = artistPageSource.RegexMatchesGroups(@"""tal qx"".+?href=""(?<url>.+?)"">(?<trackName>.+?)<")
                    .Select(g => new {URL = "https://www.lyrics.com" +  g["url"].Value, TrackName = g["trackName"].Value})
                    .Where(x => x.TrackName.Contains(SearchTrackName, StringComparison.InvariantCultureIgnoreCase)) // TODO standardize this better
                    .ToArray();

                if (!trackLinks.Any())
                {
                    continue;
                }

                foreach (var trackLink in trackLinks)
                {
                    // Console.WriteLine("trackLink.URL: " + trackLink.URL);
                    var trackPageSource = HttpClient.GetPageSource(trackLink.URL);
                    // lyric-body
                    var lyrics = trackPageSource
                        .RegexMatches(@"""lyric-body"".+?>(?<value>.+?)</pre>", "value")
                        .Join(Environment.NewLine)
                        .Replace("<br/>", Environment.NewLine)
                        .Trim()
                        ;
                    // lyrics = System.Web.HttpUtility.HtmlDecode(lyrics);
                    if (lyrics == null || lyrics.Contains("(lyrics not available)"))
                        continue;

                    ArtistName = jsonToken["value"].ToString();
                    TrackName = trackLink.TrackName;
                    Lyrics = lyrics;

                    break;
                }
            }

            this.CheckedLyrics = true;
        }
    }
}

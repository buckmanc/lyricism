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
    public class BandcampExtractor : LyricExtractor
    {
        private const string SearchURL = "https://bandcamp.com/api/bcsearch_public_api/1/autocomplete_elastic";


        public BandcampExtractor(string artistName, string trackName, string? albumName = null) : base(artistName, trackName, albumName)
        {
            this.SourceName = "Bandcamp";
        }
        public override void GetLyrics()
        {
            //var postData = "{\"search_text\":\"r\",\"search_filter\":\"\",\"full_page\":false,\"fan_id\":null}";
            var postData = new Dictionary<string, string>();
            postData.Add("search_text", this.SearchArtistName + " " + this.SearchTrackName);
            postData.Add("full_page", "false");
            postData.Add("search_filter", "t");
            postData.Add("fan_id", null);

            var search = HttpClient.GetPageSource(SearchURL, postData);
            var parsedJson = JObject.Parse(search);
            // Console.WriteLine("bandcamp search response: " + search);
            var jsonToken = parsedJson.SelectToken("auto.results");
            if (jsonToken == null)
            {
                this.CheckedLyrics = true;
                return;
            }

            var items = JsonConvert.DeserializeObject<List<dynamic>>(jsonToken.ToString()) ?? new();

            foreach (var item in items)
            {
                var lyricsURL = (string)item.item_url_path;
                // Console.WriteLine(lyricsURL);
                var lyrics = HttpClient.GetPageSource(lyricsURL);
                // lyrics = lyrics.RegexMatch(@"\"lyrics\":{.+text\":\"(?<value>.+)\"}}", "value");
                lyrics = lyrics.RegexMatch(@"""lyrics"":{.+text"":""(?<value>.+)""}}", "value");
                // deserialize directly to string to handle escape sequences
                // lyrics = JsonConvert.DeserializeObject<string>(lyrics);
                if (lyrics != null)
                    lyrics = Regex.Unescape(lyrics);
                if (lyrics == null || lyrics.Contains("(lyrics not available)"))
                    continue;

                ArtistName = item.band_name;
                TrackName = item.name;
                Lyrics = lyrics.StripHTML().Trim();

                break;
            }

            this.CheckedLyrics = true;
        }
    }
}

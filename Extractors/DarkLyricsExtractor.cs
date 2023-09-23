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
    public class DarkLyricsExtractor : LyricExtractor
    {
        private const string SearchURL = "http://www.darklyrics.com/ss";


        public DarkLyricsExtractor(string artistName, string trackName) : base(artistName, trackName)
        {
            this.Order = 90;
            this.SourceName = "Dark Lyrics";
        }
        public override void GetLyrics()
        {
            var postData = new Dictionary<string, string>();
            postData.Add("q", this.SearchArtistName);

            var search = HttpClient.GetPageSource(SearchURL, postData, Program.PostDataType.Form);
            var artistNames = search
                .RegexMatches(@"CDATA\[(?<value>.+?)\]", "value")
                .Where(x => !x.IsNullOrWhiteSpace() && x.SearchTermMatch(SearchArtistName))
                .ToArray();
            if (!(artistNames?.Any() ?? false))
            {
                this.DebugLog.Add("No search results.");
                this.CheckedLyrics = true;
                return;
            }

            // Console.WriteLine(search);

            foreach (var artistName in artistNames)
            {
                this.DebugLog.Add("Artist: " + artistName);

                var formattedartistName = artistName.Replace(" ", string.Empty).UrlEncode();
                var artistURL = "http://www.darklyrics.com/" + formattedartistName.First() + "/" + formattedartistName + ".html";
                var artistPageSource = HttpClient.GetPageSource(artistURL);
                var trackLinks = artistPageSource.RegexMatchesGroups(@"href=""(\.\.)?(?<url>.+?lyrics.+?)(?<trackNumber>#\d+?)?"">(?<trackName>.+?)<")
                    .Select(g => new {
                        URL = "http://www.darklyrics.com" + g["url"].Value,
                        TrackName = g["trackName"].Value,
                        TrackNumber = g["trackNumber"].Value.Replace("#", string.Empty)
                    })
                    .Where(x => x.TrackName.SearchTermMatch(SearchTrackName))
                    .ToArray();

                if (!trackLinks.Any())
                {
                    this.DebugLog.Add("No matching track found.");
                    continue;
                }

                foreach (var trackLink in trackLinks)
                {
                    this.DebugLog.Add("Track: " + trackLink.TrackName);

                    var albumPageSource = HttpClient.GetPageSource(trackLink.URL);
                    // lyric-body
                    var tracks = albumPageSource
                        .RegexMatchesGroups(@"name=""(?<trackNumber>\d+?)"".+?\n(?<lyrics>.+?)(<div|<h3>)")
                        .Select(g => new
                        {
                            TrackNumber = g["trackNumber"].Value,
                            Lyrics = g["lyrics"].Value,
                        })
                        .ToArray();

                    var track = tracks.Where(t => t.TrackNumber ==  trackLink.TrackNumber).FirstOrDefault();
                    if (track?.Lyrics == null || track.Lyrics.ContainsAny(LyricErrors))
                    {
                        this.DebugLog.Add("No lyrics found.");
                        continue;
                    }

                    ArtistName = artistName.ToUpper(); // dark lyrics does not have properly capitalized artist names
                    TrackName = trackLink.TrackName;
                    Lyrics = track.Lyrics;

                    break;
                }
            }

            this.CheckedLyrics = true;
        }
    }
}

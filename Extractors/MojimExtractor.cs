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
    public class MojimExtractor : LyricExtractor
    {
        private const string SearchURL = "http://www.mojim.com/";
        private const string MojimLinkyPattern = @"href=""(?<url>.+?)"" title=""(?<title>.+?) lyrics""";


        public MojimExtractor(string artistName, string trackName) : base(artistName, trackName)
        {
            this.Order = 60;
            this.SourceName = "Mojim";
        }
        public override void GetLyrics()
        {
            var search = HttpClient.GetPageSource(SearchURL + SearchArtistName.UrlEncode() + ".html?u1");
            var artists = search
                .RegexMatchesGroups(MojimLinkyPattern)
                .Select(g => new {
                    ArtistName = g["title"].Value,
                    URL = "https://mojim.com" + g["url"].Value,
                })
                .Where(x => !x.ArtistName.IsNullOrWhiteSpace() && x.ArtistName.SearchTermMatch(SearchArtistName))
                .ToArray();

            if (!(artists?.Any() ?? false))
            {
                this.DebugLog.Add("No search results.");
                this.CheckedLyrics = true;
                return;
            }

            foreach (var artist in artists)
            {
                var artistName = artist.ArtistName;
                this.DebugLog.Add("Artist: " + artistName);

                // get the "songs sorted by song name" page
                var artistSongsUrl = artist.URL.RegexReplace(@"\.htm$", "-A2.htm");
                var artistPageSource = HttpClient.GetPageSource(artistSongsUrl);
                var trackLinks = artistPageSource.RegexMatchesGroups(MojimLinkyPattern)
                    .Select(g => new {
                        URL = "http://www.mojim.com" + g["url"].Value,
                        TrackName = g["title"].Value,
                    })
                    .Where(x => x.TrackName.SearchTermMatch(SearchTrackName))
                    .ToArray();

                if (!trackLinks.Any())
                {
                    this.DebugLog.Add("No matching tracks found.");
                    continue;
                }

                foreach (var trackLink in trackLinks)
                {
                    var trackName = trackLink.TrackName;
                    this.DebugLog.Add("Track: " + trackName);

                    var lyricsRegex =
                        @"<dl[^\r\n]+?\n" +
                        Regex.Escape(artistName) +
                        @".+?" +
                        Regex.Escape(trackName) +
                        @".+?\n(?<value>.+?)</dl>"
                        ;

                    // Console.WriteLine("trackLink.URL: " + trackLink.URL);
                    var songSource = HttpClient.GetPageSource(trackLink.URL);
                    // lyric-body
                    var obfuscatedLyrics = songSource
                        .RegexMatch(lyricsRegex, "value");

                    var lyrics =
                        obfuscatedLyrics.Replace("<br />", "\n")
                        .RegexReplace(@"(&#\d+)", "$1;") // correct html character encoding
                        .HtmlDecode() // decode characters
                        .Split("\n")
                        .Where(line => !line.Contains("mojim.com", StringComparison.InvariantCultureIgnoreCase))
                        .Join("\n")
                        ;

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
            }

            this.CheckedLyrics = true;
        }
    }
}

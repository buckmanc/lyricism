using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lyricism
{
    public abstract class LyricExtractor
    {
        public string SourceName { get; set; } = "abstract class";
        public string SubSourceName { get; set; }
        public int Order { get; set; } = 0;
        internal string[] LyricErrors = new string[]
        {
            "lyrics not available", 
            "sometimes the lyrics are just not submitted to us yet",
            "sometimes the lyrics are just not submited to us yet", //sic
        };
        public bool IsInstrumental()
        {
            return IsInstrumental(this.Lyrics);
        }

        private static string[] instrumentalIndicators = new string[] {"inst", "instrumental"};
        private static bool IsInstrumental(string value)
        {
            var comparisonText = value.AlphanumericOnly().ToLower();

            return instrumentalIndicators.Contains(comparisonText);
        }

        private string _Lyrics;
        public string Lyrics 
        { 
            get
            {
                if (!CheckedLyrics) GetLyrics();
                return _Lyrics;
            }
            set
            {
                if (value != null)
                    value = value.StripHTML().StandardizeSpaces().Trim();

                if (IsInstrumental(value))
                    value = "Instrumental";

                _Lyrics = value;
            }
        }
        private string _ArtistName;
        public string ArtistName 
        { 
            get
            {
                if (!CheckedLyrics) GetLyrics();
                return _ArtistName;
            }
            set
            {
                if (value != null)
                    value = value.StandardizeSpaces().Trim();

                _ArtistName = value;
            }
        }
        private string _TrackName;
        public string TrackName 
        { 
            get
            {
                if (!CheckedLyrics) GetLyrics();
                return _TrackName;
            }
            set
            {
                if (value != null)
                    value = value.StandardizeSpaces().Trim();

                _TrackName = value;
            }
        }

        public bool Active = true;
        public bool CheckedLyrics = false;
        public bool IsCache = false;
        public string SearchArtistName { get; set; }
        public string SearchTrackName { get; set; }

        // public List<string> DebugLog { get; set; } = new();
        public List<string> DebugLog = new();


        private Lazy<LoggingHttpClient> _httpClient;
        public LoggingHttpClient HttpClient 
        { 
            get
            {
                return _httpClient.Value;
            }
        }
        public LyricExtractor(string artistName, string trackName)
        {
            this.SearchArtistName = artistName;
            this.SearchTrackName = trackName;
            this._httpClient = new Lazy<LoggingHttpClient>(() =>
            {
                var httpClient = new LoggingHttpClient(ref this.DebugLog);
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (X11; Linux x86_64; rv:95.0) Gecko/20100101 Firefox/95.0");
                // httpClient.DefaultRequestHeaders.Add("referer", "google.com");
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                return httpClient;
            });

        }

        public abstract void GetLyrics();
        public void Cache()
        {
            if (!this.CheckedLyrics || String.IsNullOrWhiteSpace(this.Lyrics) || this.IsCache)
                return;

            var artistName = this.ArtistName.Sanitize();
            var trackName = this.TrackName.Sanitize();
            var sourceName = this.SourceName.Sanitize();
            var cachePath = System.IO.Path.Join(Program.LyricsCacheDir,artistName, trackName, sourceName + ".txt");
            var cacheDir = System.IO.Path.GetDirectoryName(cachePath);
            if (!System.IO.Directory.Exists(cacheDir))
                System.IO.Directory.CreateDirectory(cacheDir);

            System.IO.File.WriteAllText(cachePath, this.Lyrics);
        }

        private Dictionary<string, string> translations = new(); 
        public string GetTranslation(string languageCode)
        {
            if (this.translations.TryGetValue(languageCode, out var transLyrics))
                return transLyrics;

            if (string.IsNullOrWhiteSpace(this.Lyrics))
                return this.Lyrics;

            System.Diagnostics.Process p = System.Diagnostics.Process.Start("trans", "- brief -no-ansi -target " + languageCode + " -- \"" + Lyrics + "\"");
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;

            p.Start();

            string output = p.StandardOutput.ReadToEnd();
            string error = p.StandardError.ReadToEnd();

            p.WaitForExit();

            // TODO what error code indicates failure?
            int exitCode = p.ExitCode;

            if (true)
            {
                this.translations.Add(languageCode, output);

                Console.WriteLine(output);
                Environment.Exit(10);

                return output;
            }
            else
            {
                this.translations.Add(languageCode, null);
                return null;
            }
        }
    }
}

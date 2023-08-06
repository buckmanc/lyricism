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
                _TrackName = value;
            }
        }
        private string _AlbumName;
        public string AlbumName 
        { 
            get
            {
                if (!CheckedLyrics) GetLyrics();
                return _AlbumName;
            }
            set
            {
                _AlbumName = value;
            }
        }

        public bool Active = true;
        public bool CheckedLyrics = false;
        public bool IsCache = false;
        public string SearchArtistName { get; set; }
        public string SearchTrackName { get; set; }
        public string SearchAlbumName { get; set; }

        private Lazy<HttpClient> _httpClient = new Lazy<HttpClient>(() => {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (X11; Linux x86_64; rv:95.0) Gecko/20100101 Firefox/95.0");
            // httpClient.DefaultRequestHeaders.Add("referer", "google.com");
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            return httpClient;
            });
        public HttpClient HttpClient 
        { 
            get
            {
                return _httpClient.Value;
            }
        }
        public LyricExtractor(string artistName, string trackName, string? albumName = null)
        {
            this.SearchArtistName = artistName;
            this.SearchTrackName = trackName;
            this.SearchAlbumName = albumName;
        }

        public abstract void GetLyrics();
        public void Cache()
        {
            if (!this.CheckedLyrics || String.IsNullOrWhiteSpace(this.Lyrics) || this.IsCache)
                return;

            var artistName = this.ArtistName.Sanitize();
            var trackName = this.TrackName.Sanitize();
            var sourceName = this.SourceName.Sanitize();
            var cachePath = System.IO.Path.Join(Program.CacheDir,artistName, trackName, sourceName + ".txt");
            var cacheDir = System.IO.Path.GetDirectoryName(cachePath);
            if (!System.IO.Directory.Exists(cacheDir))
                System.IO.Directory.CreateDirectory(cacheDir);

            System.IO.File.WriteAllText(cachePath, this.Lyrics);
        }
    }
}

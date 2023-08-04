using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lyricism
{
    public abstract class LyricExtractor
    {
        public string SourceName { get; set;  }
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
        public string SearchArtistName { get; set; }
        public string SearchTrackName { get; set; }
        public string SearchAlbumName { get; set; }

        private Lazy<HttpClient> _httpClient = new Lazy<HttpClient>(() => {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (X11; Linux x86_64; rv:95.0) Gecko/20100101 Firefox/95.0");
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
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lyricism.Models
{
    internal class CurrentlyPlayingDeets
    {
        public string ArtistName { get; set; }
        public string TrackName { get; set; }
        public string PodcastDescription { get; set; }
        public string Errors { get; set; }
        public bool IsEpisode { get; set; }
        public double PercentProgress { get; set; }
        public int ProgressMs { get; set; }
        public int DurationMs { get; set; }

        public string CompareID
        {
            get
            {
                return (this?.ArtistName ?? string.Empty)
                    + " - "
                    + (this?.TrackName ?? string.Empty)
                    ;
            }
        }

        public bool IsError
        {
            get
            {
                return !string.IsNullOrWhiteSpace(this.Errors);
            }
        }
        // for compatibility with lyric getting
        public IEnumerable<string> PodcastDescriptionArray
        {
            get
            {
                if (string.IsNullOrWhiteSpace(this.PodcastDescription))
                    return null;
                return new string[] { this.PodcastDescription };
            }
        }
    }
}

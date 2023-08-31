using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lyricism.Models
{
    internal class SpotifyToken
    {
        public string? ClientID { get; set; }
        public string? ClientSecret { get; set; }
        public string? UserAccessToken { get; set; }
        public string? UserRefreshToken { get; set; }
    }

    internal class SpotifyTokens : List<SpotifyToken>
    {

        public void Serialize()
        {
            var dir = Path.GetDirectoryName(Program.TokensPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using var stream = File.Create(Program.TokensPath);
            System.Text.Json.JsonSerializer.Serialize(stream, this);
        }
        public static SpotifyTokens Deserialize()
        {
            if (File.Exists(Program.TokensPath))
            {
                using var stream = File.OpenRead(Program.TokensPath);
                var output = System.Text.Json.JsonSerializer.Deserialize<SpotifyTokens>(stream);
                return output;
            }
            return null;

        }
    }
}

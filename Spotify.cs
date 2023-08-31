using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using SpotifyAPI.Web.Http;
using lyricism.Models;

namespace lyricism
{
    internal static class Spotify
    {
        private static EmbedIOAuthServer _server;
        private static string CodeReceived;
        private static string ErrorReceived;

        internal static void AddSpotifyAccount()
        {
            var tokens = SpotifyTokens.Deserialize() ?? new SpotifyTokens();

            // TODO test if they're valid? ask if the user wants to update them if they exist?

            var token = new SpotifyToken();
            token.ClientID = tokens.Select(t => t.ClientID).FirstOrDefault();
            token.ClientSecret = tokens.Select(t => t.ClientSecret).FirstOrDefault();

            List<string> ips = new List<string>();

            System.Net.IPHostEntry entry = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());

            foreach (System.Net.IPAddress ip in entry.AddressList)
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    ips.Add(ip.ToString());
            ips = ips.OrderByDescending(x => x.Split(".").First()).ToList();
            var targetIP = ips.FirstOrDefault() ?? "localhost";
            var port = 5543;
            var callbackURL = "http://" + targetIP + ":" + port.ToString("####") + "/callback";

            //Console.WriteLine("machine IPs: " + ips.Join(", "));
            var staticIPWarning = string.Empty;
            if (targetIP != "localhost")
                staticIPWarning = " Unless you have a static IP set up for this machine, be aware that this URL could change in the future.";

            if (string.IsNullOrWhiteSpace(token.ClientID) || string.IsNullOrWhiteSpace(token.ClientID))
            {
                Console.WriteLine("If you don't have Spotify developer token already please go to https://developer.spotify.com/dashboard/create and create them. Be sure to use a callback address of " + callbackURL + "." + staticIPWarning);
                Console.WriteLine();
                Console.WriteLine("Please enter your Spotify Developer Client ID:");
                token.ClientID = Console.ReadLine();
                Console.WriteLine("Please enter your Spotify Developer Client Secret:");
                token.ClientSecret = Console.ReadLine();
            }

            // https://johnnycrazy.github.io/SpotifyAPI-NET/docs/authorization_code/

            _server = new EmbedIOAuthServer(new Uri(callbackURL), port);
            _server.Start().Wait();

            _server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;
            _server.ErrorReceived += OnErrorReceived;

            var request = new LoginRequest(_server.BaseUri, token.ClientID, LoginRequest.ResponseType.Code)
            {
                Scope = new List<string> {
                    Scopes.UserModifyPlaybackState
                        , Scopes.UserReadPlaybackState
                        , Scopes.UserReadCurrentlyPlaying
                        , Scopes.UserLibraryModify
                }
            };

            var tinyURL = MakeTinyUrl(request.ToUri().ToString());
            Console.WriteLine("Please login to Spotify and authorize this app here: " + tinyURL);

            // TODO this should really be async stuff
            while (string.IsNullOrWhiteSpace(CodeReceived) && string.IsNullOrWhiteSpace(ErrorReceived))
                System.Threading.Thread.Sleep(1000);

            _server.Stop().Wait();

            if (!string.IsNullOrWhiteSpace(ErrorReceived))
            {
                Console.WriteLine("Error received while authenticating: " + ErrorReceived);
                return;
            }

            var config = SpotifyClientConfig.CreateDefault();
            var tokenResponse = new OAuthClient(config).RequestToken(
                    new AuthorizationCodeTokenRequest(
                        token.ClientID, token.ClientSecret, CodeReceived, new Uri(callbackURL)
                        )
                    ).Result;

            token.UserRefreshToken = tokenResponse.RefreshToken;
            token.UserAccessToken = tokenResponse.AccessToken;
            tokens.Add(token);
            tokens.Serialize();

        }

        private static async Task OnAuthorizationCodeReceived(object sender, AuthorizationCodeResponse response)
        {
            CodeReceived = response.Code;
        }

        private static async Task OnErrorReceived(object sender, string error, string state)
        {
            ErrorReceived = error;
        }

        //stackoverflow.com/a/366135
        public static string MakeTinyUrl(string url)
        {
            try
            {
                if (!url.ToLower().StartsWith("http") && !url.ToLower().StartsWith("ftp"))
                {
                    url = "http://" + url;
                }
                if (url.Length <= 30)
                {
                    return url;
                }

                using (var client = new HttpClient())
                using (var result = client.GetAsync("http://tinyurl.com/api-create.php?url=" + url).Result)
                {
                    if (!result.IsSuccessStatusCode)
                        return null;

                    string text;
                    using (var reader = new StreamReader(result.Content.ReadAsStream()))
                    {
                        text = reader.ReadToEnd();
                    }
                    return text;
                }
            }
            catch (Exception)
            {
                return url;
            }
        }

        private static List<Lazy<SpotifyClient>> _Clients;

        public static List<Lazy<SpotifyClient>> Clients
        {
            get 
            {
                _Clients ??= GetClients();
                return _Clients;
            }
        }

        // private static int i = 0;
        private static List<Lazy<SpotifyClient>> GetClients()
        {
            var tokens = SpotifyTokens.Deserialize();

            if (tokens == null || !tokens.Any())
                return null;
            
            var outClients = new List<Lazy<SpotifyClient>>();

            foreach(var token in tokens)
            {
                var outClient = new Lazy<SpotifyClient>(() =>
                {
                // i+=1;
                // Console.WriteLine("lazy client get: " + i.ToString());

                    var response = new OAuthClient().RequestToken(
                          new AuthorizationCodeRefreshRequest(
                              token.ClientID,
                              token.ClientSecret,
                              token.UserRefreshToken
                              )
                        ).Result.CloneToTokenResponse();


                    token.UserAccessToken = response.AccessToken;

                    var config = SpotifyClientConfig
                      .CreateDefault()
                      // according to documentation this should allow for auto token refresh
                      .WithAuthenticator(new AuthorizationCodeAuthenticator(token.ClientID, token.ClientSecret, response))
                      //.WithToken(token.UserAccessToken)
                      ;

                    return new SpotifyClient(config);
                });
                outClients.Add(outClient);
            }
            return outClients;
        }

        public static CurrentlyPlayingDeets GetCurrentlyPlayingDeets()
        {

            var currentlyPlaying = Spotify.Clients
                .Select(client => client.Value.Player.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest(PlayerCurrentlyPlayingRequest.AdditionalTypes.All)).Result)
                .FirstOrDefault(playing => playing?.IsPlaying ?? false);
            var playableItem = currentlyPlaying?.Item;

            var episode = playableItem as FullEpisode;
            var track = playableItem as FullTrack;
            CurrentlyPlayingDeets output = null;

            if (track != null)
            {
                output = new CurrentlyPlayingDeets()
                {
                    IsEpisode = false,
                    ArtistName = track.Artists.First().Name, // the first artist is always the primary
                    TrackName = track.Name,
                    ProgressMs = currentlyPlaying.ProgressMs ?? 0,
                    DurationMs = track.DurationMs,
                };
            }
            else if (episode != null)
            {
                output = new CurrentlyPlayingDeets()
                {
                    IsEpisode = true,
                    ArtistName = episode.Show.Name,
                    TrackName = episode.Name,
                    ProgressMs = currentlyPlaying.ProgressMs ?? 0,
                    DurationMs = episode.DurationMs,
                    // TODO display description instead of lyrics for podcasts
                    PodcastDescription = episode.Description,
                    // Errors = "Cannot retrive lyrics for a podcast."
                };
                // TODO theoretically if this were a music podcast
                // track details *could* be discerned by parsing the show notes and looking at the track progress
                // not sure the juice is worth the squeeze, as it were
            }
            else
            {
                output = new CurrentlyPlayingDeets()
                {
                    Errors = "Spotify isn't playing."
                };
            }

            return output;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using lyricism.Models;

namespace lyricism
{
    internal static class Watcher
    {
        private static CurrentlyPlayingDeets CurrentTrack { get; set; }
        private static string Lyrics { get; set; } = string.Empty;
        private static Stopwatch ErrorTimer = new();
        private static Stopwatch NoInputTimer = Stopwatch.StartNew();
        private static int ScrollOffset;
        private static bool AutoScrollOn = true;
        private static TimeSpan ErrorTimeout = TimeSpan.FromHours(1);
        private static TimeSpan NoInputTimeout = TimeSpan.FromHours(6);

        private static string lastLyric;
        private static int lastScrollOffset;
        private static int lastH;
        private static int lastW;
        private static void UpdateScreen()
        {
            var h = Console.WindowHeight;
            // var w = Console.WindowWidth;

            // format handles width
            var formattedLyrics = Program.FormatLyricReport(Watcher.Lyrics);
            var lyLines = formattedLyrics.Split("\n");

            var minScroll = 0;
            var maxScroll = lyLines.Count() - Console.WindowHeight;
            // var maxScroll = lyLines.Count();
            if (maxScroll < 0)
                maxScroll = 0;

            // reset scroll offset to within bounds if it's outside of bounds
            if (ScrollOffset > maxScroll)
                ScrollOffset = maxScroll;
            else if (ScrollOffset < minScroll)
                ScrollOffset = minScroll;

            // back out if nothing has actually changed
            // saves performance, reduces blinking on some platforms

            if (lastLyric == Watcher.Lyrics
                && lastScrollOffset == Watcher.ScrollOffset
                && lastH == Console.WindowHeight
                && lastW == Console.WindowWidth)
                return;

            lastLyric = Watcher.Lyrics;
            lastScrollOffset = Watcher.ScrollOffset;
            lastH = Console.WindowHeight;
            lastW = Console.WindowWidth;

            // handle scroll offset and chop to height
            lyLines = lyLines.Skip(ScrollOffset).ToArray();
            lyLines = lyLines.Take(h).ToArray();

            // we only use \n internally, but need to use the environment appropriate line ending otherwise
            var output = lyLines.Join(Environment.NewLine);

            // these ansi fixes are probably a sign that a refactor is needed
            var ansiRegex = @"\x1B\[(?<value>[\d;]+)?m";
            var ansiCodes = output.RegexMatches(ansiRegex, "value");
            // if the end of a color/style section is cut off, slap a reset on the end of the string
            // otherwise the whole terminal ends up styled
            // it's zero width, so doesn't screw up the fit
            if (ansiCodes.Any() && ansiCodes.Last() != "0")
                output += Program.ansiReset;

            // amend the opposite problem, where the starting code is cut off
            if (ansiCodes.Any() && ansiCodes.First() == "0")
                output = formattedLyrics.RegexMatch(ansiRegex) + output;

            // do we need to move the cursor too?
            // causes blinking on some platforms, but overwriting everything with spaces has drawbacks
            Console.Clear();
            Console.Write(output);
        }
        internal static void Watch(string site, bool noCache, bool verbose)
        {
            // spin off threads to watch for updates
            var watchSpot = Task.Run(() => WatchSpotify(site, noCache, verbose));
            var watchScreen = Task.Run(() => WatchScreen());
            var watchKey = Task.Run(() => WatchKeyboard());

            watchKey.Wait();
        }

        internal static void WatchKeyboard()
        { 
            // loop until escape key
            ConsoleKeyInfo? keyInfo;
            do
            {
                while (!Console.KeyAvailable) 
                    System.Threading.Thread.Sleep(250);
                keyInfo = Console.ReadKey(true);

                int updatedScrollOffset = ScrollOffset;

                if (keyInfo.Value.Key == ConsoleKey.Home
                        || (keyInfo.Value.Key == ConsoleKey.UpArrow &&
                            keyInfo.Value.Modifiers == ConsoleModifiers.Control))
                    updatedScrollOffset = 0;
                else if (keyInfo.Value.Key == ConsoleKey.End
                        || (keyInfo.Value.Key == ConsoleKey.DownArrow &&
                            keyInfo.Value.Modifiers == ConsoleModifiers.Control))
                    updatedScrollOffset = int.MaxValue;
                else if (keyInfo.Value.Key == ConsoleKey.UpArrow)
                    updatedScrollOffset -= 1;
                else if (keyInfo.Value.Key == ConsoleKey.DownArrow)
                    updatedScrollOffset += 1;
                else if (keyInfo.Value.Key == ConsoleKey.PageUp)
                    updatedScrollOffset -= Console.WindowHeight;
                else if (keyInfo.Value.Key == ConsoleKey.PageDown)
                    updatedScrollOffset += Console.WindowHeight;
                else if (keyInfo.Value.Key == ConsoleKey.A)
                    AutoScrollOn = !AutoScrollOn;

                NoInputTimer.Restart();

                if (updatedScrollOffset != ScrollOffset)
                {
                    ScrollOffset = updatedScrollOffset;
                    AutoScrollOn = false;
                    UpdateScreen();
                }

            } while(1 == 1
                    && (!keyInfo.HasValue || !keyInfo.Value.Key.In(ConsoleKey.Escape, ConsoleKey.Q))
                    && ErrorTimer.Elapsed < ErrorTimeout
                    && NoInputTimer.Elapsed < NoInputTimeout
                    );
        }

        private static void WatchSpotify(string site, bool noCache, bool verbose)
        {
            var checkTrack = Spotify.GetCurrentlyPlayingDeets();

            // loop checking the current track forever
            do
            {
                // check for the song to updated periodically
                // wait a bit before updating the screen for errors
                // sometimes spotify reports that it isn't playing when it is
                // this way, time will pass after pausing where it sits on the last lyric displayed
                while (
                        // (CurrentTrack?.CompareID ?? string.Empty) == (checkTrack?.CompareID ?? string.Empty) 
                        (CurrentTrack?.CompareID ?? string.Empty) == (checkTrack?.CompareID ?? string.Empty) 
                        || (!string.IsNullOrWhiteSpace(Watcher.Lyrics) &&  checkTrack.IsError && ErrorTimer.Elapsed.TotalSeconds < 45)
                        )
                {
                    // wait longer the longer the spotify isn't playing or some other error occurs
                    // the point of this is to save API hits
                    var waitMs = 0;
                    if (ErrorTimer.Elapsed.TotalMinutes < 1)
                        waitMs = 10 * 1000;
                    else if (ErrorTimer.Elapsed.TotalMinutes < 5)
                        waitMs = 30 * 1000;
                    else if (ErrorTimer.Elapsed.TotalHours < 1)
                        waitMs = 60 * 1000;
                    else
                        // if spotify hasn't been playing for more than an hour I really doubt the user expects to come back to this app and find it running
                        waitMs = 60 * 5 * 1000;

                    System.Threading.Thread.Sleep(waitMs);
                    checkTrack = Spotify.GetCurrentlyPlayingDeets();
                    // TODO pass in track progress percent?
                    AutoScroll(checkTrack.ProgressMs, checkTrack.DurationMs);

                    if (!(checkTrack?.IsError ?? true) && ErrorTimer.IsRunning)
                        ErrorTimer.Reset();
                    else if ((checkTrack?.IsError ?? true) && !ErrorTimer.IsRunning)
                        ErrorTimer.Start();
                }

                CurrentTrack = checkTrack;

                Lyrics = string.Empty;
                AutoScrollOn = true;

                if (CurrentTrack?.IsError ?? true)
                {
                    Lyrics += CurrentTrack?.Errors ?? "Erroneous Spotify state.";
                    UpdateScreen();
                    continue;
                }

                foreach (var blurb in CurrentTrack.PodcastDescriptionArray ?? Program.GetLyricReport(CurrentTrack.ArtistName, CurrentTrack.TrackName, site, noCache, verbose))
                {
                    var blurbx = blurb;
                    // hacky but efficient
                    if (!blurbx.StartsWith("\r"))
                        blurbx += "\n"; 
                    Lyrics += blurbx;
                    UpdateScreen();
                }


            } while (true);
        }
        private static void AutoScroll(int ProgressMs, int DurationMs)
        {
            if (!AutoScrollOn)
                return;

            var adj = 40 * 1000;
            var durAdj = DurationMs - adj;
            var progAdj = ProgressMs - (adj / 4);
            if (durAdj < 0)
                return;
            if (progAdj < 0)
                progAdj = 0;

            var progPerc = ((double)progAdj) / durAdj;

            // scroll offset is only measured from the top of the screen
            var lyLines = Lyrics.Split("\n").Count() - Console.WindowHeight;

            var updatedScrollOffset = (int)(progPerc * lyLines);

            if (updatedScrollOffset != ScrollOffset)
            {
                ScrollOffset = updatedScrollOffset;
                UpdateScreen();
            }
        }
        private static void WatchScreen()
        {
            var lastH = Console.WindowHeight;
            var lastW = Console.WindowWidth;
            var checkH = lastH;
            var checkW = lastW;
            do
            {
                // check for the song to updated periodically
                while (lastH == checkH && lastW == checkW)
                {
                    System.Threading.Thread.Sleep(1000);
                    checkH = Console.WindowHeight;
                    checkW = Console.WindowWidth;
                }

                checkH = lastH;
                checkW = lastW;
                UpdateScreen();

            } while (true);
        }
    }
}

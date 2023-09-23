using Newtonsoft.Json;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace lyricism
{
    public static class ExtensionMethods
    {
        private const string timeoutErrorMessage = "http request timed out";
        public static string GetPageSource(this HttpClient httpClient, string url, Dictionary<string, string> postData = null, Program.PostDataType postDataType = Program.PostDataType.Json)
        {
            var httpLoggins = httpClient as LoggingHttpClient;
            if (httpLoggins != null)
                httpLoggins.DebugLog.Add(url);

            System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> response;

            if (postData == null)
            {
                response = httpClient.GetAsync(url);
            }
            else
            {
                HttpContent httpContent = null;
                if (postDataType == Program.PostDataType.Json)
                {
                    string requestJson = JsonConvert.SerializeObject(postData);
                    httpContent = new StringContent(requestJson, Encoding.UTF8, "application/json");
                }
                else if (postDataType == Program.PostDataType.Form)
                    httpContent = new FormUrlEncodedContent(postData);

                response = httpClient.PostAsync(url, httpContent);
            }

            var output = string.Empty;

            // handle timeouts gracefully
            try
            {
                output = response.Result.Content.ReadAsStringAsync().Result;
            }
            catch (System.Threading.Tasks.TaskCanceledException)
            {
                if (httpLoggins != null)
                    httpLoggins.DebugLog.Add(timeoutErrorMessage);
                else
                    Console.WriteLine(timeoutErrorMessage);
            }
            catch (System.AggregateException ex) when (ex.InnerException is System.Threading.Tasks.TaskCanceledException)
            {
                if (httpLoggins != null)
                    httpLoggins.DebugLog.Add(timeoutErrorMessage);
                else
                    Console.WriteLine(timeoutErrorMessage);
            }

            return output;
        }

        public static string RegexMatch(this string source, string regex, string? groupName = null)
        {
            return source.RegexMatches(regex, groupName).FirstOrDefault();
        }
        public static string[] RegexMatches(this string source, string regex, string? groupName = null)
        {
            var matchesGroups = source.RegexMatchesGroups(regex);

            if (!matchesGroups.Any())
                return new string[]{};

            if (groupName == null)
                return matchesGroups.Select(g => g[0].Value).ToArray();

            return matchesGroups.Select(g => g[groupName].Value).ToArray();

        }

        private static RegexOptions regexOptions = RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnoreCase;
        public static List<System.Text.RegularExpressions.GroupCollection>  RegexMatchesGroups(this string source, string regex)
        {
            if (source == null) return new ();

            var matches = new Regex(regex, regexOptions).Matches(source);

            if (!matches.Any())
                return new ();

            return matches.Select(m => m.Groups).ToList();

        }

        public static string RegexReplace(this string source, string regex, string replacement)
        {
            if (source.IsNullOrWhiteSpace()) return source;

            return new Regex(regex, regexOptions).Replace(source, replacement);
        }

        public static string StripHTML(this string input)
        {
            return Regex.Replace(input, "<.*?>", String.Empty);
        }
        /// <summary>
        /// Returns a string of the contained elements joined with the specified separator.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        /// emulates an old VB function, which was very convenient
        public static string Join(this IEnumerable<string> value, string separator)
        {
            return string.Join(separator, value.ToArray());
        }
        /// <summary>
        /// Returns a string of the contained elements joined.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// emulates an old VB function, which was very convenient
        public static string Join(this IEnumerable<string> value)
        {
            return string.Join(string.Empty, value.ToArray());
        }
        public static string Join(this IEnumerable<char> value)
        {
            return string.Join(string.Empty, value.ToArray());
        }
        public static string Sanitize(this string value)
        {
            var invalidChars = System.IO.Path.GetInvalidFileNameChars();
            var output = value.Where(c => !invalidChars.Contains(c)).Join();
            return output;
        }
        // bad form, but enables convenient chaining
        public static string[] GetDirectories(this string value)
        {
            var output = System.IO.Directory.GetDirectories(value);
            return output;
        }
        public static string[] GetFiles(this string value)
        {
            var output = System.IO.Directory.GetFiles(value);
            return output;
        }

        // the source of these two objects is identical, aside from the names
        // cloning allows for auto refresh
        public static AuthorizationCodeTokenResponse CloneToTokenResponse(this AuthorizationCodeRefreshResponse value)
        {
            var output = new AuthorizationCodeTokenResponse()
            {
                AccessToken = value.AccessToken,
                TokenType = value.TokenType,
                ExpiresIn = value.ExpiresIn,
                Scope = value.Scope,
                RefreshToken = value.RefreshToken,
                CreatedAt = value.CreatedAt
            };

            return output;
        }

        public static string Repeat(this string value, int n)
        {
            return string.Join(string.Empty, Enumerable.Repeat(value, n));
        }

        // https://stackoverflow.com/a/3165188
        public static bool In<T>(this T item, params T[] items)
        {
            if (items == null)
                throw new ArgumentNullException("items");

            return items.Contains(item);
        }

        /// <summary>
        /// Removes the specified string from the beginning of a string.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="trimString"></param>
        /// <param name="comparisonType"></param>
        /// <returns></returns>
        public static string TrimStart(this string value, string trimString = " ", StringComparison comparisonType = StringComparison.InvariantCultureIgnoreCase)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(trimString)) return value;

            while (value.StartsWith(trimString, comparisonType))
            {
                value = value.Substring(trimString.Length);
            }

            return value;

        }

        /// <summary>
        /// Removes the specified string from the beginning of a string.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="trimString"></param>
        /// <param name="comparisonType"></param>
        /// <returns></returns>
        public static string TrimEnd(this string value, string trimString = " ", StringComparison comparisonType = StringComparison.InvariantCultureIgnoreCase)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(trimString)) return value;

            while (value.EndsWith(trimString, comparisonType))
            {
                value = value.Substring(0, value.Length - trimString.Length);
            }

            return value;

        }
        public static string Trim(this string value, string trimString = " ", StringComparison comparisonType = StringComparison.InvariantCultureIgnoreCase)
        {

            value = value.TrimStart(trimString, comparisonType);
            value = value.TrimEnd(trimString, comparisonType);

            return value;
        }

        public static string StandardizeSpaces(this string value)
        {
            if (value == null)
                return value;
            var weirdSpaces = new string[] { "\u200B", "\u202F", "\u00A0" };
            var output = value.Replace(weirdSpaces, " ");
            return output;
        }
        public static string Replace(this string value, IEnumerable<string> oldStrings, string newString)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            foreach (var oldString in oldStrings)
            {
                value = value.Replace(oldString, newString);
            }

            return value;
        }
        public static bool ContainsAny(this string value, IEnumerable<string> searchy, StringComparison sc = StringComparison.InvariantCultureIgnoreCase)
        {
            return searchy.Any(x => value.Contains(x, sc));
        }

        public static bool IsNullOrWhiteSpace(this string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        private static char[] _wildcards = new char[] { '*', '?' };
        public static string AlphanumericOnly(this string value, bool preserveWildcards = false)
        {
            return value.Where(c =>
                char.IsLetterOrDigit(c) ||
                (preserveWildcards && _wildcards.Contains(c))
            ).Join();
        }

        public static string RemoveAccents(this string text)
        {
            StringBuilder sbReturn = new StringBuilder();
            var arrayText = text.Normalize(NormalizationForm.FormD).ToCharArray();
            foreach (char letter in arrayText)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(letter) != UnicodeCategory.NonSpacingMark)
                    sbReturn.Append(letter);
            }
            return sbReturn.ToString();
        }

        public static string Standardize(this string value)
        {
            return value
                .Replace(" ", " ") // replace nbsp with regular space
                .Replace("​", "")   // replace whatever the hell this is with a space
                .Trim()
                .TrimStart("the ")
                .Replace(" & ", " and ")
                .RemoveAccents()
                .AlphanumericOnly(preserveWildcards: true)
                .ToLower()
                ;
        }
        
        //https://www.dotnetperls.com/levenshtein
        public static int LevenshteinDistance(this string s, string t)
        {
            s = s.Standardize();
            t = t.Standardize();

            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // Verify arguments.
            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            // Initialize arrays.
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }

            // Begin looping.
            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    // Compute cost.
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
                }
            }
            // Return cost.
            return d[n, m];
        }

        public static double LevenshteinPercentChange(this string s, string t)
        {
            var dist = s.LevenshteinDistance(t);
            var perc = dist * 1.0 / Math.Max(s.Length, t.Length);
            return perc;
        }

        public static bool SearchTermMatch(this string value, string searchTerm)
        {
            var valueStan = value.Standardize();
            var searchTermStan = searchTerm.Standardize();

            return 1 == 2
                || valueStan.Contains(searchTermStan)
                || searchTermStan.Contains(valueStan)
                || valueStan.LevenshteinPercentChange(searchTermStan) <= 0.25
                ;

        }

        public static string UrlEncode(this string value)
        {
            return System.Web.HttpUtility.UrlEncode(value);
        }

        public static string HtmlDecode(this string value)
        {
            return System.Web.HttpUtility.HtmlDecode(value);
        }
    }
}

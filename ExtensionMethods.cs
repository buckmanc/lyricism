using Newtonsoft.Json;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public static List<System.Text.RegularExpressions.GroupCollection>  RegexMatchesGroups(this string source, string regex)
        {
            if (source == null) return new (); // List<RegularExpressions.GroupCollection>();

            var options = RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnoreCase;

            var matches = new Regex(regex, options).Matches(source);

            if (!matches.Any())
                return new (); //List<RegularExpressions.GroupCollection>();


            return matches.Select(m => m.Groups).ToList();

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
    }
}

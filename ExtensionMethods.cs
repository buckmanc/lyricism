using Newtonsoft.Json;
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
        public static string GetPageSource(this HttpClient httpClient, string url, Dictionary<string, string> postData = null, Program.PostDataType postDataType = Program.PostDataType.Json)
        {
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
                Console.WriteLine("http request timed out");
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
    }
}

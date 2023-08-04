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
        public static string GetPageSource(this HttpClient httpClient, string url, Dictionary<string, string>? postData = null)
        {
            if (postData == null)
            {
                using (var result = httpClient.GetAsync(url).Result)
                    return result.Content.ReadAsStringAsync().Result;
            }
            else
            {
                //var content = new FormUrlEncodedContent(postData.ToArray());
                string requestJson = JsonConvert.SerializeObject(postData);
                HttpContent httpContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

                using (var result = httpClient.PostAsync(url, httpContent).Result)
                    return result.Content.ReadAsStringAsync().Result;
            }
        }

        public static string RegexMatch(this string source, string regex, string? groupName = null)
        {
            return source.RegexMatches(regex, groupName).FirstOrDefault();
        }
        public static string[] RegexMatches(this string source, string regex, string? groupName = null)
        {
            if (source == null) return new string[]{};

            var options = RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnoreCase;

            var matches = new Regex(regex, options).Matches(source);

            if (!matches.Any())
                return new string[]{};

            if (groupName == null)
                return matches.Select(m => m.Value).ToArray();

            return matches.Select(m => m.Groups[groupName].Value).ToArray();

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
    }
}

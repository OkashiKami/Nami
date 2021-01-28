﻿using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Nami.Modules.Misc.Common;
using Nami.Services;

namespace Nami.Modules.Misc.Services
{
    public class MemeGenService : NamiHttpService
    {
        public const string Provider = "memegen.link";
        private const string ApiEndpoint = "https://api.memegen.link";

        private static readonly Regex _whitespaceRegex = new Regex(@"\s+", RegexOptions.Compiled);
        private static readonly ImmutableDictionary<char, string> _replacements = new Dictionary<char, string> {
            {'?', "~q"},
            {'&', "~a"},
            {'%', "~p"},
            {'#', "~h"},
            {'/', "~s"},
            {'\\', "~b"},
            {' ', "-"},
            {'-', "--"},
            {'_', "__"},
            {'"', "''"}
        }.ToImmutableDictionary();


        public override bool IsDisabled => false;


        public static string GenerateMemeUrl(string template, string? topText, string? bottomText)
            => $"{ApiEndpoint}/images/{Sanitize(template)}/{Sanitize(topText)}/{Sanitize(bottomText)}.jpg?font=impact";

        public static async Task<MemeTemplate?> GetMemeTemplateAsync(string template)
        {
            try {
                string json = await _http.GetStringAsync($"{ApiEndpoint}/templates/{Sanitize(template)}").ConfigureAwait(false);
                return JsonConvert.DeserializeObject<MemeTemplate>(json);
            } catch (HttpRequestException) {
                return null;
            }
        }

        public static async Task<IReadOnlyList<MemeTemplate>> GetMemeTemplatesAsync()
        {
            string json = await _http.GetStringAsync($"{ApiEndpoint}/templates/").ConfigureAwait(false);
            List<MemeTemplate> data = JsonConvert.DeserializeObject<List<MemeTemplate>>(json);
            return data.AsReadOnly();
        }


        private static string Sanitize(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "_";

            input = _whitespaceRegex.Replace(input, " ");

            var sb = new StringBuilder();
            foreach (char c in input) {
                if (_replacements.TryGetValue(c, out string? tmp))
                    sb.Append(tmp);
                else
                    sb.Append(c);
            }

            return sb.ToString();
        }
    }
}

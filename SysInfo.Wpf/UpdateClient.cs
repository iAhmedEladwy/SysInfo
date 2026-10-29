using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CNSysInfo
{
    internal sealed class UpdateClient
    {
        private static HttpClient Client(TimeSpan timeout)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            var client = new HttpClient { Timeout = timeout };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("ELADWYSOFT-SysInfo/" + typeof(UpdateClient).Assembly.GetName().Version.ToString(3));
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            return client;
        }

        public async Task<AppUpdate> CheckAsync(Version current, string skipped)
        {
            using (var client = Client(TimeSpan.FromSeconds(8)))
            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8)))
            using (var response = await client.GetAsync("https://api.github.com/repos/" + UpdateLogic.Repository + "/releases/latest",
                HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false))
            {
                if (response.StatusCode == HttpStatusCode.NotFound) return null;
                response.EnsureSuccessStatusCode();
                using (var source = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var buffer = new MemoryStream())
                {
                    await CopyBounded(source, buffer, 1024 * 1024, timeout.Token).ConfigureAwait(false);
                    buffer.Position = 0;
                    var release = (GitHubRelease)new DataContractJsonSerializer(typeof(GitHubRelease)).ReadObject(buffer);
                    return UpdateLogic.Find(release, current, skipped);
                }
            }
        }

        private static async Task CopyBounded(Stream source, Stream destination, long maximum, CancellationToken token)
        {
            var buffer = new byte[81920]; long total = 0; int read;
            while ((read = await source.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false)) != 0)
            {
                total += read;
                if (total > maximum) throw new InvalidDataException("The response exceeds its expected size.");
                await destination.WriteAsync(buffer, 0, read, token).ConfigureAwait(false);
            }
        }
    }
}

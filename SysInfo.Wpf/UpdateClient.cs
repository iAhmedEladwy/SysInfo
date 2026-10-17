using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace CNSysInfo
{
    internal sealed class UpdateClient
    {
        public static bool IsStandalone
        {
            get
            {
#if NETFRAMEWORK
                return false;
#else
                return true;
#endif
            }
        }

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
                    return UpdateLogic.Find(release, current, skipped, IsStandalone);
                }
            }
        }

        public async Task<string> DownloadAsync(AppUpdate update, string directory)
        {
            using (var client = Client(TimeSpan.FromMinutes(5)))
            using (var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5)))
            {
                string hash = update.Sha256;
                if (!UpdateLogic.ValidHash(hash))
                {
                    using (var response = await client.GetAsync(update.ChecksumUrl, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false))
                    {
                        response.EnsureSuccessStatusCode();
                        using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                        using (var memory = new MemoryStream())
                        {
                            await CopyBounded(stream, memory, 1024, timeout.Token).ConfigureAwait(false);
                            hash = System.Text.Encoding.UTF8.GetString(memory.ToArray()).Trim().Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0];
                        }
                    }
                }
                if (!UpdateLogic.ValidHash(hash)) throw new InvalidDataException("The release checksum is invalid.");
                Directory.CreateDirectory(directory);
                string package = Path.Combine(directory, update.PackageName);
                using (var response = await client.GetAsync(update.PackageUrl, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using (var destination = File.Create(package))
                        await CopyBounded(stream, destination, update.PackageSize, timeout.Token).ConfigureAwait(false);
                }
                if (new FileInfo(package).Length != update.PackageSize) throw new InvalidDataException("The download is incomplete.");
                using (var sha = SHA256.Create())
                using (var stream = File.OpenRead(package))
                {
                    string actual = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
                    if (!actual.Equals(hash, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("The download checksum does not match the release.");
                }
                return package;
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

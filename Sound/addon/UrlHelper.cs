using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SystemX.Addon {
    public sealed class UrlHelper {
        private UrlHelper() {
        }

        private static System.Net.Http.HttpClient s_client;
        private static object urlLock = new object();

        public static System.Net.Http.HttpClient GetHttpClient() {
            if (s_client == null) {
                lock (urlLock) {
                    if (s_client == null) {
                        s_client = new System.Net.Http.HttpClient();
                    }
                }
            }
            return s_client;
        }

        public static Stream openStream(Uri url) {
            if (url == null) {
                throw new ArgumentNullException(nameof(url));
            }
            if (url.IsFile) {
                return new FileStream(url.LocalPath, FileMode.Open, FileAccess.Read);
            }
            System.Net.Http.HttpClient client = GetHttpClient();
            byte[] buffer = client.GetByteArrayAsync(url).Result;
            return new MemoryStream(buffer);

            //return OpenStreamAsync(url).Result;

            //WebClient client = new WebClient();
            //byte[] buffer = client.DownloadData(url);
            //client.Dispose();
            //return new MemoryStream(buffer);
        }

        private static async Task<Stream> OpenStreamAsync(Uri url) {
            System.Net.Http.HttpClient client = GetHttpClient();

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(new TimeSpan(0, 0, 0, 0, 5000));
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            var ms = new MemoryStream();
            var response = await client.GetAsync(url, cancellationToken);
            if (response.IsSuccessStatusCode) {
                await response.Content.CopyToAsync(ms);
                ms.Position = 0;
            }
            return ms;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADD_Launcher
{
    internal class HttpClientHandler
    {
        public readonly HttpClient httpClient;

        public HttpClientHandler()
        {
            httpClient = new HttpClient();
        }

        public async Task<string> GetResponseAsync(string queryString)
        {
            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(queryString);
                response.EnsureSuccessStatusCode();
                string responseText = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Response From Server: " + responseText);
                return responseText;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine("HTTP request error: " + ex.Message);
                return null;
            }
        }
    }
}

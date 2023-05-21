using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WagemasterEvents.Models;

namespace WagemasterEvents.Database
{
    public class ApiHelper
    {
        private static HttpClient client = new HttpClient();

        public async Task<List<Event>> FetchEventsFromApiAsync(string server, string username, string password)
        {
            List<Event> eventsList = new List<Event>();

            try
            {
                string url = $"http://{server}:7080/api/Events";
                Debug.WriteLine($"Fetching events from {url}");

                // Creating user
                var user = new Userx
                {
                    Username = username,
                    Password = password
                };

                // Serializing user object to JSON
                var json = JsonSerializer.Serialize(user);

                // Converting JSON to HttpContent
                var data = new StringContent(json, Encoding.UTF8, "application/json");

                // Sending a POST request
                HttpResponseMessage response = await client.PostAsync(url, data);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                // Deserializing response to a list of events
                eventsList = JsonSerializer.Deserialize<List<Event>>(responseBody);
                Debug.WriteLine($"Fetched {eventsList.Count} events from API");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching events from API: {ex.Message}");
            }

            return eventsList;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using WagemasterEvents.Models;

namespace WagemasterEvents.Database
{
    public class ApiHelper
    {
        private static HttpClient client = new HttpClient();

        public async Task<List<Event>> FetchEventsFromApiAsync(string server)
        {
            List<Event> eventsList = new List<Event>();

            try
            {
                HttpResponseMessage response = await client.GetAsync($"{server}:7080/API/Events");
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                eventsList = JsonSerializer.Deserialize<List<Event>>(responseBody);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching events from API: {ex.Message}");
            }

            return eventsList;
        }
    }
}


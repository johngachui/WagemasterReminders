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
                //Debug.WriteLine($"Fetching events from {url}");

                // Creating user
                var user = new Userx
                {
                    Username = username,
                    Password = password
                };

                // Serializing user object to JSON
                var json = JsonSerializer.Serialize(user);
                //Debug.WriteLine($"Get json = {json}");
                // Converting JSON to HttpContent
                var data = new StringContent(json, Encoding.UTF8, "application/json");

                // Sending a POST request
                HttpResponseMessage response = await client.PostAsync(url, data);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                // Deserializing response to a list of events
                eventsList = JsonSerializer.Deserialize<List<Event>>(responseBody);
                //Debug.WriteLine($"Fetched {eventsList.Count} events from API");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching events from API: {ex.Message}");
            }

            return eventsList;
        }


        public async Task<bool> UpdateEventAsync(string server, string username, string password, Event eventToUpdate)
        {
            try
            {
                string url = $"http://{server}:7080/api/Events/update/{eventToUpdate.ID}";
                //Debug.WriteLine($"Updating event at {url}");

                // Creating user
                var user = new Userx
                {
                    Username = username,
                    Password = password
                };
                // Replace single backslash with double backslash in the DatabasePath
                string updatedDatabasePath = eventToUpdate.DatabasePath.Replace("\\", "\\\\");
                // Prepare the event to be updated
                var updatedEvent = new
                {
                    DatabasePath = updatedDatabasePath,
                    Dismissed = eventToUpdate.Dismissed
                    
                };

                // Merge user and updated event into one object for sending to API
                var updateUserAndEvent = new
                {
                    user.Username,
                    user.Password,
                    updatedEvent.Dismissed,
                    updatedEvent.DatabasePath
                };

                //Debug.WriteLine($"Log UpdateEventAsync: username = {username} and password = {password} and eventToUpdate.ID = {eventToUpdate.ID} and eventToUpdate.Dismissed = {eventToUpdate.Dismissed} and updatedDatabasePath = {updatedDatabasePath}");
                try
                {
                    // Serializing object to JSON
                    var json = JsonSerializer.Serialize(updateUserAndEvent);
                    //Debug.WriteLine(json);
                    // Converting JSON to HttpContent
                    var data = new StringContent(json, Encoding.UTF8, "application/json");
                    //Debug.WriteLine($"Log3:{url} and {data}");
                    // Sending a PUT request
                    HttpResponseMessage response = await client.PostAsync(url, data);
                    response.EnsureSuccessStatusCode();

                    return response.IsSuccessStatusCode;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error1 updating event via API: {ex.Message}");
                    Debug.WriteLine($"Exception details: {ex.ToString()}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error2 updating event via API: {ex.Message}");
                return false;
            }
        }

    }


}

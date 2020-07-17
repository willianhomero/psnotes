using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using PSNotes.Models;
using PSNotes.Models.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace PSNotes.Services
{
    public class ApiNoteStorageService : INoteStorageService
    {
        private readonly ILogger<EventHubsEventPublisher> _logger;
        private readonly string _serviceEndpoint;

        public ApiNoteStorageService(IOptions<StorageApiSettings> settings, ILogger<EventHubsEventPublisher> logger)
        {
            _logger = logger;

            _serviceEndpoint = settings.Value.ServiceEndpoint;
        }

        public async Task<Note> GetNote(string username, string noteId)
        {
            using (var client = new HttpClient())
            {
                string url = $"{_serviceEndpoint}/api/notes/{username}/{noteId}";

                var response = await client.GetAsync(url);

                string responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<Note>(responseContent);
                }

                throw new Exception(responseContent);
            }
        }

        public async Task SaveNote(Note note)
        {
            using (var client = new HttpClient())
            {
                string url = $"{_serviceEndpoint}/api/notes/{note.UserId}";

                var response = await client.PostAsync(url, new StringContent(JsonConvert.SerializeObject(note),Encoding.UTF8,"application/json"));

                string responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return;
                }

                throw new Exception(responseContent);
            }
        }

        public async Task DeleteNote(string username, string noteId)
        {
            using (var client = new HttpClient())
            {
                string url = $"{_serviceEndpoint}/api/notes/{username}/{noteId}";

                var response = await client.DeleteAsync(url);

                string responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return;
                }

                throw new Exception(responseContent);
            }
        }

        public async Task<List<NoteSummary>> GetNoteList(string username)
        {
            using (var client = new HttpClient())
            {
                string url = $"{_serviceEndpoint}/api/notes/{username}";

                var response = await client.GetAsync(url);

                string responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<List<NoteSummary>>(responseContent);
                }

                throw new Exception(responseContent);
            }
        }
    }
}

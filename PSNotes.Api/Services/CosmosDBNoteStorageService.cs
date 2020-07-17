using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PSNotes.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using PSNotes.Api.Models.Settings;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Linq;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents;

namespace PSNotes.Api.Services
{
    public class CosmosDbNoteStorageService : INoteStorageService
    {
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheEntryOptions;
        private readonly ILogger _logger;
        private readonly CosmosDbSettings _cosmosDbSettings;
        private readonly DocumentClient _client;

        public CosmosDbNoteStorageService(IOptions<CosmosDbSettings> cosmosDbSettings, IDistributedCache cache, DistributedCacheEntryOptions cacheEntryOptions, ILoggerFactory loggerFactory)
        {
            _cache = cache;
            _cacheEntryOptions = cacheEntryOptions;
            _logger = loggerFactory.CreateLogger<CosmosDbNoteStorageService>();
            _cosmosDbSettings = cosmosDbSettings.Value;

            _client = new DocumentClient(new Uri(_cosmosDbSettings.Endpoint), _cosmosDbSettings.Key);

            CreateDatabaseIfNotExistsAsync().Wait();
            CreateCollectionIfNotExistsAsync().Wait();
        }

        public async Task SaveNote(Note note)
        {
            if (note == null) throw new ArgumentNullException("note");

            if (string.IsNullOrWhiteSpace(note.UserId)) throw new ArgumentException("The note provided didn't have a user ID assigned");

            try
            {
                Document document = await _client.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri(_cosmosDbSettings.DatabaseId, _cosmosDbSettings.CollectionId), note);

                // re-assign the note id as it was assigned by CosmosDB
                note.Id = document.Id;

                string noteObjectKey = GetNoteObjectKey(note.UserId, document.Id);

                // Save data in cache.
                _cache.SetString(noteObjectKey, JsonConvert.SerializeObject(note), _cacheEntryOptions);

                // Update summary
                List<NoteSummary> notes = GetNoteList(note.UserId);
                NoteSummary oldNote = notes.SingleOrDefault(n => n.NoteId == document.Id);

                if (notes != null)
                {
                    notes.Remove(oldNote);
                }

                notes.Add(new NoteSummary { NoteId = note.Id, UserId = note.UserId, Title = note.Title, CreatedAt = note.CreatedAt });

                SaveNoteList(note.UserId, notes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured while saving a note.");

                throw;
            }
        }

        public async Task<Note> GetNote(string username, string noteId)
        {
            if (string.IsNullOrWhiteSpace(username)) throw new ArgumentNullException("username");
            if (noteId == null) throw new ArgumentNullException("noteId");

            string noteObjectKey = GetNoteObjectKey(username, noteId);

            // Look for note in cache.
            string cacheEntry = _cache.GetString(noteObjectKey);

            if (string.IsNullOrEmpty(cacheEntry))
            {
                try
                {
                    Document document = await _client.ReadDocumentAsync(UriFactory.CreateDocumentUri(_cosmosDbSettings.DatabaseId, _cosmosDbSettings.CollectionId, noteId));
                    Note note = (Note)(dynamic)document;

                    // Save data in cache.
                    _cache.SetString(noteObjectKey, JsonConvert.SerializeObject(note), _cacheEntryOptions);

                    return note;
                }
                catch (DocumentClientException e)
                {
                    if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        return null;
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            // the note was found in cache
            return JsonConvert.DeserializeObject<Note>(cacheEntry);
        }

        public async Task DeleteNote(string username, string noteId)
        {
            if (string.IsNullOrWhiteSpace(username)) throw new ArgumentNullException("username");
            if (noteId == null) throw new ArgumentNullException("noteId");

            string noteObjectKey = GetNoteObjectKey(username, noteId);

            try
            {
                await _client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(_cosmosDbSettings.DatabaseId, _cosmosDbSettings.CollectionId, noteId));

                // Remove data from cache.
                _cache.Remove(noteObjectKey);

                // Update summary
                List<NoteSummary> notes = GetNoteList(username);
                NoteSummary oldNote = notes.SingleOrDefault(n => n.NoteId == noteId);

                if (notes != null)
                {
                    notes.Remove(oldNote);

                    // Remove data from cache.
                    _cache.Remove(noteObjectKey);
                }

                SaveNoteList(username, notes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured while deleting a note.");

                return;
            }
        }

        public List<NoteSummary> GetNoteList(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) throw new ArgumentNullException("username");

            string summaryObjectKey = GetSummaryObjectKey(username);

            // Look for cache key.
            string cacheEntry = _cache.GetString(summaryObjectKey);

            if (string.IsNullOrEmpty(cacheEntry))
            {
                try
                {
                    var notes = from n
                                in _client.CreateDocumentQuery<Note>(UriFactory.CreateDocumentCollectionUri(_cosmosDbSettings.DatabaseId, _cosmosDbSettings.CollectionId))
                                where n.UserId == username
                                select new NoteSummary { NoteId = n.Id, UserId = n.UserId, Title = n.Title, CreatedAt = n.CreatedAt };

                    // Save data in cache.
                    _cache.SetString(summaryObjectKey, JsonConvert.SerializeObject(notes), _cacheEntryOptions);

                    return notes.ToList();
                }
                catch (Exception)
                {
                    //�Not�found�if�we�get�an�exception
                    return new List<NoteSummary>();
                }
            }

            return JsonConvert.DeserializeObject<List<NoteSummary>>(cacheEntry);
        }

        private void SaveNoteList(string username, List<NoteSummary> notes)
        {
            if (notes == null) throw new ArgumentNullException("notes");

            if (notes.Any())
            {
                // check that notes are for a single user
                var groups = notes.GroupBy(n => n.UserId);

                if (groups.Count() > 1) throw new ArgumentException("The list of note refers to more than one user.");
            }

            string summaryObjectKey = GetSummaryObjectKey(username);

            try
            {
                // Save data in cache.
                _cache.SetString(summaryObjectKey, JsonConvert.SerializeObject(notes), _cacheEntryOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured while saving a note list.");

                throw;
            }
        }

        private string GetSummaryObjectKey(string username)
        {
            return $"{username}/summary";
        }


        private string GetNoteObjectKey(string username, string noteId)
        {
            return $"{username}/{noteId}";
        }

        private async Task CreateDatabaseIfNotExistsAsync()
        {
            try
            {
                await _client.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(_cosmosDbSettings.DatabaseId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await _client.CreateDatabaseAsync(new Database { Id = _cosmosDbSettings.DatabaseId });
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task CreateCollectionIfNotExistsAsync()
        {
            try
            {
                await _client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(_cosmosDbSettings.DatabaseId, _cosmosDbSettings.CollectionId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await _client.CreateDocumentCollectionAsync(
                        UriFactory.CreateDatabaseUri(_cosmosDbSettings.DatabaseId),
                        new DocumentCollection { Id = _cosmosDbSettings.CollectionId },
                        new RequestOptions { OfferThroughput = 1000 });
                }
                else
                {
                    throw;
                }
            }
        }
    }
}

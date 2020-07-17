using System;
using Newtonsoft.Json;

namespace PSNotes.Models
{
    public class Note
    {
        public string UserId { get; set; }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

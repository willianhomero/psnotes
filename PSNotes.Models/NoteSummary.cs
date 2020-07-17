using System;

namespace PSNotes.Models
{
    public class NoteSummary
    {
        public string UserId { get; set; }
        public string NoteId { get; set; }
        public string Title { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

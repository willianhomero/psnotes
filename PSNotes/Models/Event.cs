using System;

namespace PSNotes.Models
{
    public class Event
    {
        public EventType EventType { get; set; }
        public string NoteId { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Username { get; set; }
    }
}

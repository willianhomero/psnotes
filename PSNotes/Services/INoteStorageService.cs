using PSNotes.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PSNotes.Services
{
    public interface INoteStorageService
    {
        Task<Note> GetNote(string username, string noteId);
        Task SaveNote(Note note);
        Task DeleteNote(string username, string noteId);
        Task<List<NoteSummary>> GetNoteList(string username);
    }
}

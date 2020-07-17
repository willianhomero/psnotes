using PSNotes.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PSNotes.Api.Services
{
    public interface INoteStorageService
    {
        Task<Note> GetNote(string username, string noteId);
        Task SaveNote(Note note);
        Task DeleteNote(string username, string noteId);
        List<NoteSummary> GetNoteList(string username);
    }
}

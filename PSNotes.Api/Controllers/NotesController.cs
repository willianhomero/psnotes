using PSNotes.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PSNotes.Api.Services;

namespace PSNotes.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotesController : ControllerBase
    {
        private readonly INoteStorageService _noteStorageService;
        private readonly ILogger<NotesController> _logger;

        public NotesController(INoteStorageService noteStorageService, ILogger<NotesController> logger)
        {
            _noteStorageService = noteStorageService;
            _logger = logger;
        }

        [HttpGet("{username}/{id}")]
        public async Task<ActionResult<Note>> Get(string username, string id)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return BadRequest("Username is required");
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest("Note ID is required");
            }

            Note note = await _noteStorageService.GetNote(username, id);

            if (note == null)
            {
                _logger.LogWarning($"Couldn't find note with ID '{id}' for user '{username}'");
                return NotFound();
            }

            _logger.LogInformation($"Getting note with ID '{id}' for user '{username}'");

            return note;
        }

        [HttpPost("{username}")]
        public async Task<ActionResult<Note>> Post([FromRoute] string username, [FromBody] Note note)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return BadRequest("Username is required");
            }

            if (string.IsNullOrWhiteSpace(note.Id))
            {
                note.CreatedAt = DateTime.UtcNow;

                _logger.LogInformation($"Creating new note for user '{username}'");
            }
            else
            {
                // check the owner of the note
                Note originalNote = await _noteStorageService.GetNote(username, note.Id);

                if (originalNote == null)
                {
                    _logger.LogWarning($"Couldn't find note with ID '{note.Id}' for user '{username}'");
                    return NotFound();
                }

                _logger.LogInformation($"Saving changes to existing note with ID '{note.Id}' for user '{username}'");
            }

            // reset the user name as we were not displaying it on the page on purpose
            note.UserId = username;

            await _noteStorageService.SaveNote(note);

            return note;
        }

        [HttpGet("{username}")]
        public ActionResult<List<NoteSummary>> GetAll(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return BadRequest("Username is required");
            }

            _logger.LogWarning($"Getting note list for user '{username}'");

            List<NoteSummary> notes = _noteStorageService.GetNoteList(username);

            return notes;
        }

        [HttpDelete("{username}/{id}")]
        public async Task<ActionResult> Delete(string username, string id)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return BadRequest("Username is required");
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest("Note ID is required");
            }

            // check the owner of the note
            Note originalNote = await _noteStorageService.GetNote(username, id);

            if (originalNote == null)
            {
                _logger.LogWarning($"Couldn't find note with ID '{id}' for user '{username}'");
                return NotFound();
            }

            await _noteStorageService.DeleteNote(username, id);

            _logger.LogInformation($"Deleting note with ID '{id}' for user '{username}'");

            return Ok();
        }

        [HttpGet("error")]
        public ActionResult<Note> Error()
        {
            throw new Exception("This is a sample 500 error");
        }
    }
}

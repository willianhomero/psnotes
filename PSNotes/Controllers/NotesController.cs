using PSNotes.Models;
using PSNotes.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PSNotes.Controllers
{
    [Authorize]
    public class NotesController : Controller
    {
        private readonly INoteStorageService _noteStorageService;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<NotesController> _logger;

        public NotesController(INoteStorageService noteStorageService, IEventPublisher eventPublisher, ILogger<NotesController> logger)
        {
            _noteStorageService = noteStorageService;
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult New()
        {
            _logger.LogInformation("Creating new note");

            ViewData["Title"] = "Create a New Note";

            return View("Edit");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            Note note = await _noteStorageService.GetNote(User.Identity.Name, id);

            if (note == null)
            {
                _logger.LogWarning($"Couldn't find note with ID '{id}' for user '{User.Identity.Name}'");
                return NotFound();
            }

            _logger.LogInformation($"Editing note with ID '{id}' for user '{User.Identity.Name}'");

            _eventPublisher.PublishEvent(new Event
            {
                EventType = EventType.NoteViewed,
                TimeStamp = DateTime.UtcNow,
                Username = User.Identity.Name,
                NoteId = note.Id
            });

            ViewData["Title"] = $"Edit Note - {note.Title}";

            return View("Edit", note);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Note note)
        {
            EventType eventType = EventType.NoteEdited;

            if (string.IsNullOrWhiteSpace(note.Id))
            {
                //note.Id = Guid.NewGuid();
                note.CreatedAt = DateTime.UtcNow;
                eventType = EventType.NoteCreated;

                _logger.LogInformation($"Creating new note for user '{User.Identity.Name}'");
            }
            else
            {
                // check the owner of the note
                Note originalNote = await _noteStorageService.GetNote(User.Identity.Name, note.Id);

                if (originalNote == null)
                {
                    _logger.LogWarning($"Couldn't find note with ID '{note.Id}' for user '{User.Identity.Name}'");
                    return NotFound();
                }

                _logger.LogInformation($"Saving changes to existing note with ID '{note.Id}' for user '{User.Identity.Name}'");
            }

            // reset the user name as we were not displaying it on the page on purpose
            note.UserId = User.Identity.Name;

            await _noteStorageService.SaveNote(note);

            _eventPublisher.PublishEvent(new Event
            {
                EventType = eventType,
                TimeStamp = DateTime.UtcNow,
                Username = User.Identity.Name,
                NoteId = note.Id
            });

            return RedirectToAction("List");
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            ViewData["Title"] = "Your Notes";

            _logger.LogWarning($"Getting note list for user '{User.Identity.Name}'");

            List<NoteSummary> notes = await _noteStorageService.GetNoteList(User.Identity.Name);

            return View(notes);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            // check the owner of the note
            Note originalNote = await _noteStorageService.GetNote(User.Identity.Name, id);

            if (originalNote == null)
            {
                _logger.LogWarning($"Coulnd't find note with ID '{id}' for user '{User.Identity.Name}'");
                return NotFound();
            }

            await _noteStorageService.DeleteNote(User.Identity.Name, id);

            _logger.LogInformation($"Deleting note with ID '{id}' for user '{User.Identity.Name}'");

            _eventPublisher.PublishEvent(new Event
            {
                EventType = EventType.NoteDeleted,
                TimeStamp = DateTime.UtcNow,
                Username = User.Identity.Name,
                NoteId = id
            });

            return RedirectToAction("List");
        }
    }
}

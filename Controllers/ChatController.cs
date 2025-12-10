using MasterServicePlatform.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using MasterServicePlatform.Web.Models.ViewModels;

namespace MasterServicePlatform.Web.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ChatController(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task<List<ChatConversationViewModel>> LoadDialogs(string currentUserId)
        {
            var allMessages = await _context.Messages
                .Where(m => m.SenderId == currentUserId || m.ReceiverId == currentUserId)
                .ToListAsync();

            var dialogs = allMessages
                .GroupBy(m => m.SenderId == currentUserId ? m.ReceiverId : m.SenderId)
                .Select(g =>
                {
                    var last = g.OrderByDescending(x => x.SentAt).First();
                    var companionId = last.SenderId == currentUserId ? last.ReceiverId : last.SenderId;
                    var user = _context.Users.FirstOrDefault(u => u.Id == companionId);

                    return new ChatConversationViewModel
                    {
                        UserId = companionId,
                        UserName = user?.FullName ?? user?.Email ?? "Unknown",
                        LastMessage = last.Text,
                        LastMessageTime = last.SentAt,
                        IsLastFromCurrent = last.SenderId == currentUserId
                    };
                })
                .OrderByDescending(d => d.LastMessageTime)
                .ToList();

            return dialogs;
        }

        public async Task<IActionResult> Conversation(string? userId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isAdmin = User.IsInRole("Admin");

            // redirect "support"
            if (userId == "support")
            {
                var admin = await _context.Users
                    .Where(u => u.Role == UserRole.Admin)
                    .FirstOrDefaultAsync();

                if (admin == null)
                    return BadRequest("No admin available.");

                return RedirectToAction("Conversation", new { userId = admin.Id });
            }

            // Load existing dialogs
            var dialogs = await LoadDialogs(currentUserId);

            // identify support (admin)
            var support = await _context.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Admin);

            // If USER or MASTER -> add Support Chat entry manually
            if (!isAdmin && support != null)
            {
                var exists = dialogs.Any(d => d.UserId == support.Id);

                if (!exists)
                {
                    dialogs.Insert(0, new ChatConversationViewModel
                    {
                        UserId = support.Id,
                        UserName = "Support Chat",
                        LastMessage = "",
                        LastMessageTime = DateTime.MinValue
                    });
                }
                else
                {
                    dialogs.First(d => d.UserId == support.Id).UserName = "Support Chat";
                }
            }

            // If ADMIN selects userId manually -> ensure chat entry exists
            if (isAdmin && !string.IsNullOrEmpty(userId))
            {
                var exists = dialogs.Any(d => d.UserId == userId);

                if (!exists)
                {
                    var target = await _context.Users.FindAsync(userId);

                    if (target != null)
                    {
                        dialogs.Insert(0, new ChatConversationViewModel
                        {
                            UserId = target.Id,
                            UserName = target.FullName ?? target.Email,
                            LastMessage = "",
                            LastMessageTime = DateTime.MinValue
                        });
                    }
                }
            }

            ViewBag.Dialogs = dialogs;


            ViewBag.Dialogs = dialogs;
            ViewBag.ClearNotification = true;

  

            

            // store support ID for chat header
            ViewBag.SupportId = support?.Id;





            // if no specific chat selected
            if (string.IsNullOrEmpty(userId))
            {
                ViewBag.OtherName = "Select chat";
                ViewBag.OtherId = "";

                if (isAdmin)
                    return View("AdminConversation", new List<Message>());

                return View(new List<Message>());
            }

            // Load messages normally
            var messages = await _context.Messages
                .Where(m =>
                    (m.SenderId == currentUserId && m.ReceiverId == userId) ||
                    (m.SenderId == userId && m.ReceiverId == currentUserId))
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            var otherUser = await _context.Users.FindAsync(userId);

            foreach (var msg in messages)
            {
                if (msg.ReceiverId == currentUserId && !msg.IsRead)
                    msg.IsRead = true;
            }

            await _context.SaveChangesAsync();


            ViewBag.OtherName = otherUser?.FullName ?? otherUser?.Email ?? "User";
            ViewBag.OtherId = userId;

            // Support ID (admin)
            ViewBag.SupportId = support?.Id;


            if (isAdmin)
                return View("AdminConversation", messages);

            return View(messages);
        }


        [HttpPost]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var msg = await _context.Messages.FindAsync(id);

            if (msg == null) return NotFound();
            if (msg.SenderId != currentUserId) return Forbid();

            _context.Messages.Remove(msg);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> EditMessage(int id, string newText)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var msg = await _context.Messages.FindAsync(id);

            if (msg == null) return NotFound();
            if (msg.SenderId != currentUserId) return Forbid();

            msg.Text = newText;
            msg.EditedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> HasUnreadSupportMessages()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var admin = await _context.Users
                .FirstOrDefaultAsync(u => u.Role == UserRole.Admin);

            if (admin == null)
                return Json(false);

            bool unread = false;

            // USER or MASTER — check unread from ADMIN
            if (currentUserId != admin.Id)
            {
                unread = await _context.Messages.AnyAsync(m =>
                    m.SenderId == admin.Id &&
                    m.ReceiverId == currentUserId &&
                    !m.IsRead);
            }
            else
            {
                // ADMIN — check unread from ANY USER
                unread = await _context.Messages.AnyAsync(m =>
                    m.SenderId != admin.Id &&
                    m.ReceiverId == admin.Id &&
                    !m.IsRead);
            }

            return Json(unread);
        }



    }
}

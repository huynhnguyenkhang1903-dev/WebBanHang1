using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Websitebanhang.Data;
using Websitebanhang.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Websitebanhang.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminSupportController : Controller
    {
        private readonly AppDbContext _context;

        public AdminSupportController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var requests = await _context.SupportRequests
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            return View(requests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status, string adminNote)
        {
            var request = await _context.SupportRequests.FindAsync(id);
            if (request == null) return NotFound();

            request.Status = status;
            request.AdminNote = adminNote;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã cập nhật trạng thái yêu cầu hỗ trợ.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var request = await _context.SupportRequests.FindAsync(id);
            if (request != null)
            {
                _context.SupportRequests.Remove(request);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa yêu cầu hỗ trợ.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

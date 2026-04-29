using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Websitebanhang.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Websitebanhang.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminReviewController : Controller
    {
        private readonly AppDbContext _context;

        public AdminReviewController(AppDbContext context)
        {
            _context = context;
        }

        // ================= DANH SÁCH =================
        public async Task<IActionResult> Index(string tab = "pending")
        {
            var pendingReviews = await _context.Reviews
                .Include(r => r.Product)
                .Include(r => r.User)
                .Where(r => !r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var reportedReviews = await _context.Reviews
                .Include(r => r.Product)
                .Include(r => r.User)
                .Where(r => r.IsReported)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var approvedReviews = await _context.Reviews
                .Include(r => r.Product)
                .Include(r => r.User)
                .Where(r => r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.ActiveTab = tab;
            ViewBag.PendingCount = pendingReviews.Count;
            ViewBag.ReportedCount = reportedReviews.Count;
            ViewBag.ApprovedCount = approvedReviews.Count;
            ViewBag.PendingReviews = pendingReviews;
            ViewBag.ReportedReviews = reportedReviews;
            ViewBag.ApprovedReviews = approvedReviews;

            return View();
        }

        // ================= DUYỆT =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return NotFound();

            review.IsApproved = true;
            review.IsHidden = false;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã duyệt bình luận!";
            return RedirectToAction("Index", new { tab = "pending" });
        }

        // ================= XÓA (TỪ CHỐI - CHƯA DUYỆT) =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return NotFound();

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã xóa bình luận!";
            return RedirectToAction("Index", new { tab = "pending" });
        }

        // ================= ẨN BÌNH LUẬN =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Hide(int id, string returnTab = "approved")
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return NotFound();

            review.IsHidden = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã ẩn bình luận!";
            return RedirectToAction("Index", new { tab = returnTab });
        }

        // ================= BỎ ẨN BÌNH LUẬN =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unhide(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return NotFound();

            review.IsHidden = false;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã hiện lại bình luận!";
            return RedirectToAction("Index", new { tab = "approved" });
        }

        // ================= XÓA VĨNH VIỄN (ĐÃ DUYỆT) =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, string returnTab = "approved")
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return NotFound();

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã xóa vĩnh viễn bình luận!";
            return RedirectToAction("Index", new { tab = returnTab });
        }

        // ================= XÓA DO BÁO CÁO =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReported(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return NotFound();

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã xóa bình luận bị báo cáo!";
            return RedirectToAction("Index", new { tab = "reported" });
        }

        // ================= BỎ QUA BÁO CÁO =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DismissReport(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return NotFound();

            review.IsReported = false;
            review.ReportReason = null;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã bỏ qua báo cáo, bình luận vẫn được giữ lại!";
            return RedirectToAction("Index", new { tab = "reported" });
        }
    }
}

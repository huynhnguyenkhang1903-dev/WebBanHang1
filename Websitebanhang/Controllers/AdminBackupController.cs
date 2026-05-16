using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Websitebanhang.Data;

namespace Websitebanhang.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminBackupController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;
        private readonly string _backupFolder;

        public AdminBackupController(IConfiguration configuration, AppDbContext context, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _context = context;
            
            // Lọc thư mục App_Data/Backups
            _backupFolder = Path.Combine(env.ContentRootPath, "App_Data", "Backups");
            if (!Directory.Exists(_backupFolder))
            {
                Directory.CreateDirectory(_backupFolder);
            }
        }

        public IActionResult Index()
        {
            var files = Directory.GetFiles(_backupFolder, "*.bak")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .Select(f => new BackupFileViewModel
                {
                    FileName = f.Name,
                    FilePath = f.FullName,
                    SizeMB = Math.Round(f.Length / 1024.0 / 1024.0, 2),
                    CreationTime = f.CreationTime
                })
                .ToList();

            return View(files);
        }

        [HttpPost]
        public async Task<IActionResult> Create()
        {
            string dbName = GetDatabaseName();
            if (string.IsNullOrEmpty(dbName))
            {
                TempData["Error"] = "Không thể lấy tên Database từ cấu hình.";
                return RedirectToAction("Index");
            }

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string backupFileName = $"backup_{dbName}_{timestamp}.bak";
            string backupPath = Path.Combine(_backupFolder, backupFileName);

            try
            {
                // Thực thi lệnh BACKUP
                string sql = $"BACKUP DATABASE [{dbName}] TO DISK = '{backupPath}'";
                await _context.Database.ExecuteSqlRawAsync(sql);

                TempData["Success"] = $"Đã tạo bản sao lưu: {backupFileName}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi sao lưu: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Restore(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return BadRequest();

            string backupPath = Path.Combine(_backupFolder, fileName);
            if (!System.IO.File.Exists(backupPath))
            {
                TempData["Error"] = "File sao lưu không tồn tại.";
                return RedirectToAction("Index");
            }

            string dbName = GetDatabaseName();
            string connectionString = _configuration.GetConnectionString("DefaultConnection") ?? "";
            
            // Đổi connection sang master để có quyền Restore đè lên database hiện tại
            string masterConnString = connectionString.Replace($"Database={dbName}", "Database=master");

            try
            {
                using (var connection = new SqlConnection(masterConnString))
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        // 1. Chuyển sang SINGLE_USER để ngắt kết nối hiện tại
                        command.CommandText = $"ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE";
                        await command.ExecuteNonQueryAsync();

                        // 2. Restore Database
                        command.CommandText = $"RESTORE DATABASE [{dbName}] FROM DISK = '{backupPath}' WITH REPLACE";
                        await command.ExecuteNonQueryAsync();

                        // 3. Trả về MULTI_USER
                        command.CommandText = $"ALTER DATABASE [{dbName}] SET MULTI_USER";
                        await command.ExecuteNonQueryAsync();
                    }
                }

                TempData["Success"] = $"Đã khôi phục dữ liệu từ bản sao lưu: {fileName}";
            }
            catch (Exception ex)
            {
                // Nếu lỗi, cố gắng set lại MULTI_USER để không bị kẹt
                try
                {
                    using (var connection = new SqlConnection(masterConnString))
                    {
                        await connection.OpenAsync();
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = $"ALTER DATABASE [{dbName}] SET MULTI_USER";
                            await command.ExecuteNonQueryAsync();
                        }
                    }
                }
                catch { /* Ignore */ }

                TempData["Error"] = $"Lỗi khi khôi phục: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Delete(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return BadRequest();

            string backupPath = Path.Combine(_backupFolder, fileName);
            if (System.IO.File.Exists(backupPath))
            {
                System.IO.File.Delete(backupPath);
                TempData["Success"] = $"Đã xóa file: {fileName}";
            }
            else
            {
                TempData["Error"] = "File không tồn tại.";
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Download(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return BadRequest();

            string backupPath = Path.Combine(_backupFolder, fileName);
            if (!System.IO.File.Exists(backupPath)) return NotFound();

            var fileBytes = System.IO.File.ReadAllBytes(backupPath);
            return File(fileBytes, "application/octet-stream", fileName);
        }

        private string GetDatabaseName()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection") ?? "";
            var builder = new SqlConnectionStringBuilder(connectionString);
            return builder.InitialCatalog; // Trả về "TestDB"
        }
    }

    public class BackupFileViewModel
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public double SizeMB { get; set; }
        public DateTime CreationTime { get; set; }
    }
}

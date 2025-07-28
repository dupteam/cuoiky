using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebLuuFile.Data;
using WebLuuFile.Models;

namespace WebLuuFile.Pages
{
    public class DeleteTrashModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteTrashModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public FileModel FileToDelete { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id.HasValue)
            {
                FileToDelete = await _context.Files.FirstOrDefaultAsync(f => f.Id == id);
                if (FileToDelete == null)
                {
                    TempData["Error"] = "File không tồn tại!";
                    return RedirectToPage("Restore");
                }
            }
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteConfirmedAsync([FromForm] Guid id)
        {
            var file = await _context.Files.Include(f => f.DownloadLogs)
                                           .FirstOrDefaultAsync(f => f.Id == id);
            if (file == null)
            {
                TempData["Error"] = "File không tồn tại!";
                return RedirectToPage("Restore");
            }

            try
            {
                if (System.IO.File.Exists(file.FilePath))
                {
                    System.IO.File.Delete(file.FilePath);
                }

                _context.DownloadLogs.RemoveRange(file.DownloadLogs);
                _context.Files.Remove(file);
                await _context.SaveChangesAsync();

                TempData["Success"] = "File đã bị xóa vĩnh viễn!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi xóa file: {ex.Message}";
            }

            return RedirectToPage("Restore");
        }

        public async Task<IActionResult> OnPostDeleteAllConfirmedAsync()
        {
            var filesToDelete = await _context.Files.Include(f => f.DownloadLogs)
                                                    .Where(f => f.IsDeleted)
                                                    .ToListAsync();
            if (!filesToDelete.Any())
            {
                TempData["Error"] = "Không có file nào để xóa!";
                return RedirectToPage("Restore");
            }

            try
            {
                foreach (var file in filesToDelete)
                {
                    if (System.IO.File.Exists(file.FilePath))
                    {
                        System.IO.File.Delete(file.FilePath);
                    }

                    _context.DownloadLogs.RemoveRange(file.DownloadLogs);
                }

                _context.Files.RemoveRange(filesToDelete);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Tất cả file đã bị xóa vĩnh viễn!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi xóa file: {ex.Message}";
            }

            return RedirectToPage("Restore");
        }
    }
}

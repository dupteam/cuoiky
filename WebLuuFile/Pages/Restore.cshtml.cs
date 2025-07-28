using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebLuuFile.Data;
using WebLuuFile.Models;

namespace WebLuuFile.Pages
{
    public class RestoreModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public RestoreModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<FileModel> DeletedFiles { get; private set; }

        public async Task OnGetAsync()
        {
            DeletedFiles = await _context.Files
                                         .Where(f => f.IsDeleted)
                                         .Select(f => new FileModel
                                         {
                                             Id = f.Id,
                                             FileName = f.FileName,
                                             FilePath = f.FilePath,
                                             UploadDate = f.UploadDate,
                                             DeletedAt = f.DeletedAt
                                         })
                                         .ToListAsync();
        }

        public async Task<IActionResult> OnPostRestoreAsync(Guid id)
        {
            var fileToRestore = await _context.Files.FirstOrDefaultAsync(f => f.Id == id && f.IsDeleted);
            if (fileToRestore == null)
            {
                TempData["Error"] = "File không tồn tại hoặc không bị xóa!";
                return RedirectToPage();
            }

            try
            {
                string trashFolder = Path.Combine(Directory.GetCurrentDirectory(), "Trash");
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "UploadedFiles");
                string fileName = Path.GetFileName(fileToRestore.FilePath);
                string originalPath = Path.Combine(uploadsFolder, fileName);
                string trashPath = Path.Combine(trashFolder, fileName);

                if (!System.IO.File.Exists(trashPath))
                {
                    TempData["Error"] = "File không tồn tại trong thư mục Trash!";
                    return RedirectToPage();
                }

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                System.IO.File.Move(trashPath, originalPath);
                fileToRestore.FilePath = originalPath;
                fileToRestore.IsDeleted = false;
                fileToRestore.DeletedAt = null;

                _context.Files.Update(fileToRestore);
                await _context.SaveChangesAsync();

                TempData["Success"] = "File đã được khôi phục thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi khôi phục file: {ex.Message}";
            }

            return RedirectToPage();
        }


        public async Task<IActionResult> OnPostRestoreAllAsync()
        {
            var filesToRestore = await _context.Files.Where(f => f.IsDeleted).ToListAsync();
            if (!filesToRestore.Any())
            {
                TempData["Error"] = "Không có file nào để khôi phục!";
                return RedirectToPage();
            }

            try
            {
                string trashFolder = Path.Combine(Directory.GetCurrentDirectory(), "Trash");
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "UploadedFiles");

                foreach (var file in filesToRestore)
                {
                    string fileName = Path.GetFileName(file.FilePath);
                    string originalPath = Path.Combine(uploadsFolder, fileName);
                    string trashPath = Path.Combine(trashFolder, fileName);

                    if (System.IO.File.Exists(trashPath))
                    {
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        System.IO.File.Move(trashPath, originalPath);
                        file.FilePath = originalPath;
                    }

                    file.IsDeleted = false;
                    file.DeletedAt = null;
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Tất cả file đã được khôi phục!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi khôi phục file: {ex.Message}";
            }

            return RedirectToPage();
        }
    }
}

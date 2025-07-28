using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebLuuFile.Data;
using WebLuuFile.Models;
using System.IO;

namespace WebLuuFile.Pages
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public FileModel File { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            File = await _context.Files.FindAsync(id);
            if (File == null)
            {
                return NotFound();
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid id)
        {
            var fileToDelete = await _context.Files.FindAsync(id);
            if (fileToDelete == null)
            {
                return NotFound();
            }

  
            fileToDelete.IsDeleted = true;
            fileToDelete.DeletedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            var currentPath = fileToDelete.FilePath;
            if (System.IO.File.Exists(currentPath))
            {
                var trashFolder = Path.Combine(Directory.GetCurrentDirectory(), "Trash");
                if (!Directory.Exists(trashFolder))
                {
                    Directory.CreateDirectory(trashFolder);
                }
                var newPath = Path.Combine(trashFolder, Path.GetFileName(currentPath));
                System.IO.File.Move(currentPath, newPath);

                fileToDelete.FilePath = newPath;
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("Index");
        }
    }
}

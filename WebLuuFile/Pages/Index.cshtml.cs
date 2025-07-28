using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebLuuFile.Data;
using WebLuuFile.Models;

namespace WebLuuFile.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<FileModel> Files { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SearchQuery { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.Files
                                .Where(f => !f.IsDeleted)
                                .Include(f => f.FileVersions)
                                .AsQueryable();

            if (!string.IsNullOrEmpty(SearchQuery))
            {
                var lowerQuery = SearchQuery.ToLower();

                bool isValidDate = DateTime.TryParse(SearchQuery, out DateTime parsedDate);

                query = query.Where(f =>
                    f.FileName.ToLower().Contains(lowerQuery) ||
                    f.FileType.ToLower().Contains(lowerQuery) ||
                    f.UploadedBy.ToLower().Contains(lowerQuery) ||
                    f.FileSize.ToString().Contains(lowerQuery) ||
                    (f.FileVersions != null && f.FileVersions.Any(v => v.VersionNumber.ToString().Contains(lowerQuery))) ||
                    (isValidDate && f.UploadDate.Date == parsedDate.Date)
                );
            }

            Files = await query.ToListAsync();
        }
    }
}

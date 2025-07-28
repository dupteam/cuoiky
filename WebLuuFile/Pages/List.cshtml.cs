using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebLuuFile.Data;
using WebLuuFile.Models;
using System.Collections.Generic;
using System.Linq;



namespace WebLuuFile.Pages
{
    [Authorize]
    public class ListModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public List<FileModel> Files { get; set; }

        public ListModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public void OnGet()
        {
            Files = _context.Files.Where(f => f.UserId == User.Identity.Name).ToList();
        }
    }
}
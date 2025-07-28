using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebLuuFile.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public RegisterModel(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "Tên người dùng không được để trống.")]
            public string UserName { get; set; }

            [Required(ErrorMessage = "Email không được để trống.")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
            public string Email { get; set; }

            [DataType(DataType.PhoneNumber)]
            [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
            public string? PhoneNumber { get; set; }

            [Required(ErrorMessage = "Mật khẩu không được để trống.")]
            [DataType(DataType.Password)]
            [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
            public string Password { get; set; }

            [Required(ErrorMessage = "Xác nhận mật khẩu không được để trống.")]
            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "Mật khẩu và xác nhận mật khẩu không khớp.")]
            public string ConfirmPassword { get; set; }
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            if (!IsValidPassword(Input.Password))
            {
                ModelState.AddModelError("Input.Password",
                    "Mật khẩu phải có ít nhất 6 ký tự, một chữ hoa, một ký tự đặc biệt và một chữ số.");
                return Page();
            }

            // Kiểm tra tồn tại UserName / Email
            if (await _userManager.FindByNameAsync(Input.UserName) != null)
            {
                ModelState.AddModelError("Input.UserName", "Tên người dùng đã tồn tại.");
                return Page();
            }
            if (await _userManager.FindByEmailAsync(Input.Email) != null)
            {
                ModelState.AddModelError("Input.Email", "Email đã được đăng ký.");
                return Page();
            }

            var user = new IdentityUser
            {
                UserName = Input.UserName,
                Email = Input.Email,
                PhoneNumber = Input.PhoneNumber
            };

            var result = await _userManager.CreateAsync(user, Input.Password);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToPage("/Index");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return Page();
        }

        private static bool IsValidPassword(string password)
        {
            var passwordPattern = new Regex(@"^(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{6,}$");
            return passwordPattern.IsMatch(password);
        }
    }
}

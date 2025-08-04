
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using WebLuuFile.Data;
using WebLuuFile.Models;
using Xunit;
using System.Linq;
using System;
using WebLuuFile.Pages;

namespace WebLuuFile.Tests
{
    public class FileServiceTests
    {
        // Khởi tạo DbContext giả (In-Memory)
        private ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        // Tạo UserManager giả cho người dùng "testuser"
        private UserManager<IdentityUser> GetFakeUserManager()
        {
            var store = new Mock<IUserStore<IdentityUser>>();
            var userManager = new Mock<UserManager<IdentityUser>>(store.Object, null, null, null, null, null, null, null, null);
            userManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("123");
            userManager.Setup(um => um.FindByIdAsync("123")).ReturnsAsync(new IdentityUser { UserName = "testuser" });
            return userManager.Object;
        }

        // Tạo file giả để upload
        private IFormFile GetMockFormFile(string fileName = "file.txt", string content = "hello world", string contentType = "text/plain")
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            return new FormFile(stream, 0, stream.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };
        }

        // Tạo PageContext với hoặc không có người dùng đăng nhập
        private PageContext GetPageContext(bool hasUser = true)
        {
            var user = hasUser
                ? new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "123") }))
                : new ClaimsPrincipal();

            return new PageContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        // ✅ Test case 1: Dữ liệu hợp lệ => phải upload thành công và lưu vào DB
        [Fact]
        public async Task OnPostAsync_WithValidData_ShouldUploadFileAndSaveToDb()
        {
            var context = GetInMemoryDbContext();
            var userManager = GetFakeUserManager();
            var model = new UploadModel(context, userManager)
            {
                UploadFile = GetMockFormFile(),
                FilePassword = "secure123",
                WatermarkText = "test watermark",
                PageContext = GetPageContext()
            };

            var result = await model.OnPostAsync();

            // ✅ Mong đợi redirect về Index
            var redirect = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("Index", redirect.PageName);

            // ✅ Kiểm tra có lưu file vào CSDL không
            var saved = context.Files.FirstOrDefault();
            Assert.NotNull(saved);
            Assert.Equal("file.txt", saved.FileName);
            Assert.Equal("testuser", saved.UploadedBy);

            // ❌ Nếu test này FAIL → hệ thống không upload thành công dù dữ liệu hợp lệ.
        }

        // ✅ Test case 2: Thiếu file => ModelState lỗi
        [Fact]
        public async Task OnPostAsync_MissingFile_ShouldReturnModelError()
        {
            var context = GetInMemoryDbContext();
            var userManager = GetFakeUserManager();
            var model = new UploadModel(context, userManager)
            {
                UploadFile = null,
                FilePassword = "123",
                WatermarkText = "wm",
                PageContext = GetPageContext()
            };

            var result = await model.OnPostAsync();

            Assert.IsType<PageResult>(result);
            Assert.False(model.ModelState.IsValid);
            Assert.True(model.ModelState.ContainsKey("UploadFile"));

            // ❌ Nếu test này FAIL → hệ thống không kiểm tra thiếu file.
        }

        // ✅ Test case 3: Không nhập mật khẩu => lỗi ModelState
        [Fact]
        public async Task OnPostAsync_EmptyPassword_ShouldReturnModelError()
        {
            var context = GetInMemoryDbContext();
            var userManager = GetFakeUserManager();
            var model = new UploadModel(context, userManager)
            {
                UploadFile = GetMockFormFile(),
                FilePassword = "",
                WatermarkText = "wm",
                PageContext = GetPageContext()
            };

            var result = await model.OnPostAsync();

            Assert.IsType<PageResult>(result);
            Assert.False(model.ModelState.IsValid);
            Assert.True(model.ModelState.ContainsKey("FilePassword"));

            // ❌ Nếu test này FAIL → hệ thống không kiểm tra mật khẩu trống.
        }

        // ✅ Test case 4: Không nhập watermark => lỗi ModelState
        [Fact]
        public async Task OnPostAsync_EmptyWatermark_ShouldReturnModelError()
        {
            var context = GetInMemoryDbContext();
            var userManager = GetFakeUserManager();
            var model = new UploadModel(context, userManager)
            {
                UploadFile = GetMockFormFile(),
                FilePassword = "123",
                WatermarkText = "",
                PageContext = GetPageContext()
            };

            var result = await model.OnPostAsync();

            Assert.IsType<PageResult>(result);
            Assert.False(model.ModelState.IsValid);
            Assert.True(model.ModelState.ContainsKey("WatermarkText"));

            // ❌ Nếu test này FAIL → hệ thống không kiểm tra watermark rỗng.
        }

        // ❌ Test case 5: Upload file .exe => hệ thống phải từ chối
        [Fact]
        public async Task OnPostAsync_InvalidFileFormat_ShouldReturnModelError()
        {
            var context = GetInMemoryDbContext();
            var userManager = GetFakeUserManager();
            var model = new UploadModel(context, userManager)
            {
                UploadFile = GetMockFormFile("file.exe", "fake binary", "application/octet-stream"),
                FilePassword = "123",
                WatermarkText = "wm",
                PageContext = GetPageContext()
            };

            var result = await model.OnPostAsync();

            Assert.IsType<PageResult>(result);
            Assert.False(model.ModelState.IsValid);

            // ❌ Nếu test này FAIL → hệ thống chưa kiểm tra định dạng file nguy hiểm (ví dụ: .exe)
        }

        // ✅ Test case 6: Upload file rỗng => không cho phép
        [Fact]
        public async Task OnPostAsync_ZeroByteFile_ShouldReturnModelError()
        {
            var context = GetInMemoryDbContext();
            var userManager = GetFakeUserManager();

            var stream = new MemoryStream(); // tạo file 0 byte
            var file = new FormFile(stream, 0, 0, "file", "empty.txt")
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/plain"
            };

            var model = new UploadModel(context, userManager)
            {
                UploadFile = file,
                FilePassword = "123",
                WatermarkText = "wm",
                PageContext = GetPageContext()
            };

            var result = await model.OnPostAsync();

            Assert.IsType<PageResult>(result);
            Assert.False(model.ModelState.IsValid);

            // ❌ Nếu test này FAIL → hệ thống chưa kiểm tra dung lượng file upload.
        }

        // ❌ Test case 7: Chưa đăng nhập mà vẫn upload được → sai
        [Fact]
        public async Task OnPostAsync_UnauthenticatedUser_ShouldReturnErrorOrRedirect()
        {
            var context = GetInMemoryDbContext();
            var userManager = GetFakeUserManager();
            var model = new UploadModel(context, userManager)
            {
                UploadFile = GetMockFormFile(),
                FilePassword = "123",
                WatermarkText = "wm",
                PageContext = GetPageContext(hasUser: false) // không có user đăng nhập
            };

            var result = await model.OnPostAsync();

            Assert.IsType<PageResult>(result); // hoặc RedirectToPageResult nếu chuyển về login

            // ❌ Nếu test này FAIL → hệ thống cho người chưa login upload là sai về bảo mật.
        }
    }
}

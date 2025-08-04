using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using WebLuuFile.Data;
using WebLuuFile.Models;
using WebLuuFile.Pages;
using Xunit;
using System.Linq;
using System;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebLuuFile.Tests
{
    public class FileServiceTests
    {
        private ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private UserManager<IdentityUser> GetFakeUserManager()
        {
            var store = new Mock<IUserStore<IdentityUser>>();
            var userManager = new Mock<UserManager<IdentityUser>>(store.Object, null, null, null, null, null, null, null, null);

            userManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("123");
            userManager.Setup(um => um.FindByIdAsync("123")).ReturnsAsync(new IdentityUser { UserName = "testuser" });

            return userManager.Object;
        }

        private IFormFile GetMockFormFile(string fileName = "file.txt", string content = "hello world", string contentType = "text/plain")
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            return new FormFile(stream, 0, stream.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };
        }

        [Fact]
        public async Task OnPostAsync_WithValidData_ShouldUploadFileAndSaveToDb()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var userManager = GetFakeUserManager();
            var model = new UploadModel(context, userManager)
            {
                UploadFile = GetMockFormFile(),
                FilePassword = "secure123",
                WatermarkText = "test watermark"
            };

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "123")
            }));
            model.PageContext = new Microsoft.AspNetCore.Mvc.RazorPages.PageContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Act
            var result = await model.OnPostAsync();

            // Assert
            var redirectResult = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("Index", redirectResult.PageName);

            var uploaded = context.Files.FirstOrDefault();
            Assert.NotNull(uploaded);
            Assert.Equal("file.txt", uploaded.FileName);
            Assert.Equal("testuser", uploaded.UploadedBy);
        }

        [Fact]
        public async Task OnPostAsync_WithMissingFile_ShouldReturnPageWithModelError()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var userManager = GetFakeUserManager();
            var model = new UploadModel(context, userManager)
            {
                UploadFile = null,
                FilePassword = "abc",
                WatermarkText = "wm"
            };

            // Act
            var result = await model.OnPostAsync();

            // Assert
            Assert.IsType<PageResult>(result);
            Assert.False(model.ModelState.IsValid);
            Assert.True(model.ModelState.ContainsKey("UploadFile"));
        }

        [Fact]
        public async Task OnPostAsync_WithEmptyPassword_ShouldReturnPageWithModelError()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var userManager = GetFakeUserManager();
            var model = new UploadModel(context, userManager)
            {
                UploadFile = GetMockFormFile(),
                FilePassword = "",
                WatermarkText = "wm"
            };

            // Act
            var result = await model.OnPostAsync();

            // Assert
            Assert.IsType<PageResult>(result);
            Assert.False(model.ModelState.IsValid);
            Assert.True(model.ModelState.ContainsKey("FilePassword"));
        }

        [Fact]
        public async Task OnPostAsync_WithEmptyWatermark_ShouldReturnPageWithModelError()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var userManager = GetFakeUserManager();
            var model = new UploadModel(context, userManager)
            {
                UploadFile = GetMockFormFile(),
                FilePassword = "123",
                WatermarkText = ""
            };

            // Act
            var result = await model.OnPostAsync();

            // Assert
            Assert.IsType<PageResult>(result);
            Assert.False(model.ModelState.IsValid);
            Assert.True(model.ModelState.ContainsKey("WatermarkText"));
        }
    }
}

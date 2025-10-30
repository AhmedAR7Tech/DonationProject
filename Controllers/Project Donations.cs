using ClsBuisnessLayer;
using ClsDataAccess;
using ClsModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using Twilio.Jwt.AccessToken;
using static ClsModel.clsModels;
using static System.Collections.Specialized.BitVector32;
using static System.Net.Mime.MediaTypeNames;

namespace Project_Donations.Controllers
{
    [Route("api/Donations")]
    [ApiController]
    public class Project_Donations : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public Project_Donations(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GenerateJwtToken(clsModels.LoginResponse user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));

            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, user.PersonID.ToString()),
            new Claim(ClaimTypes.Role, user.Role ?? "User"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["DurationInMinutes"])),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpPost("Login")]
        public ActionResult Register(clsModels.Login login)
        {
            var user = clsBuisnessLayer.LoginUser(login);

            if (user == null)
                return BadRequest("حدث خطأ أثناء التسجيل");

            // إذا المستخدم موجود بالفعل
            if (user.IsVerified == 1)
            {
                string token = GenerateJwtToken(user);
                return Ok(new
                {
                    message = "تم تسجيل الدخول بنجاح ✔️",
                    PersonID = user.PersonID,
                    OTPRequired = false,
                    Role = user.Role,
                    Token = token
                });
            }
            else // مستخدم جديد أو محتاج تحقق
            {
                return Ok(new
                {
                    message = "تم إرسال كود التفعيل إلى حسابك  فالبريد الالكتروني  ✅",
                    user.PersonID,
                    OTPRequired = true
                });
            }
        }

        [HttpPost("VerifyOTP")]
        public ActionResult<clsModels.LoginResponse> VerifyOTP(clsModels.VerifyOTP verifyOTP)
        {
            var user = clsBuisnessLayer.VerifyOTP(verifyOTP);

            if (user == null)
            {
                return BadRequest("كود التحقق غير صحيح أو المستخدم غير موجود");
            }
            string token = GenerateJwtToken(user);
            user.Token = token;
            return Ok(user);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("AddSection")]
        public ActionResult<bool> AddSections([FromForm] clsModels.AddSections addSections, IFormFile image)
        {
            string imageurl = null;

            if (image != null && image.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    image.CopyTo(stream);
                }

                // نخزن الرابط كنسبه مسار داخل السيرفر
                imageurl = $"/images/{uniqueFileName}";
            }

            bool isAdded = clsBuisnessLayer.AddSections(addSections, imageurl);
            if (isAdded)
            {
                return Ok(true);
            }
            else
            {
                return NotFound("من المتوقع ان هناك خطا ما اثناء اضافة القسم ");
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("DeleteSection")]
        public ActionResult<bool> DeleteSection(int sectionID)
        {
            bool isDeleted = clsBuisnessLayer.DeleteSection(sectionID);
            if (isDeleted)
            {
                return Ok("تم حذف القسم بنجاح");
            }
            else
            {
                return NotFound("من المتوقع ان هناك خطا ما اثناء حذف القسم ");
            }
        }

        [HttpGet("GetAllSections")]
        public ActionResult<List<clsModels.GetAllSections>> GetAllSections()
        {
            var sections = clsBuisnessLayer.GetAllSections();

            if (sections == null || sections.Count == 0)
                return NotFound("لا يوجد اقسام حاليا");

            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            sections.ForEach(s =>
            {
                if (!string.IsNullOrEmpty(s.ImageUrl))
                {
                    // لو الرابط مش فيه http/https نضيف الـ baseUrl
                    if (!s.ImageUrl.StartsWith("http"))
                        s.ImageUrl = $"{baseUrl}{s.ImageUrl}";
                }
            });

            return Ok(sections);
        }

        [HttpPost("AddToDonationCart")]
        public ActionResult<bool> AddToDonationCart(clsModels.DonationCart donationCart)
        {
            try
            {
                bool ok = clsBuisnessLayer.AddToDonationCart(donationCart);
                if (ok)
                {
                    return Ok(new { success = true });
                }
                else
                {
                    return StatusCode(500, new { success = false, message = "حدث خطأ داخلي أثناء الإضافة." });
                }
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }

            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "حدث خطأ غير متوقع." });
            }

        }

        [HttpGet("GetAllDonationCarts")]
        public ActionResult<List<clsModels.GetAllDonatonCart>>GetAllDonationCarts(int personID)
        {
            List<clsModels.GetAllDonatonCart> donationCarts = clsBuisnessLayer.GetDonationCart(personID);

            if (donationCarts != null && donationCarts.Count > 0)
            {
                var baseUrl = $"{Request.Scheme}://{Request.Host}";

                donationCarts.ForEach(s =>
                {
                    if (!string.IsNullOrEmpty(s.SectionImage))
                    {
                        // لو الرابط مش فيه http/https نضيف الـ baseUrl
                        if (!s.SectionImage.StartsWith("http"))
                            s.SectionImage = $"{baseUrl}{s.SectionImage}";
                    }
                });
                return Ok(donationCarts);
            }
            else
            {
                return NotFound("لا يوجد تبرعات في السلة حاليا");
            }
        }

        [HttpDelete("DeletItemOfCart")]
        public ActionResult<bool> DeletItemOfCart(clsModels.RemoveDonationCart remove)
        {
            bool isDeleted = clsBuisnessLayer.RemoveFromDonationCart(remove);
            if (isDeleted)
            {
                return Ok("تم حذف التبرع من السلة بنجاح");
            }
            else
            {
                return NotFound("من المتوقع ان هناك خطا ما اثناء حذف التبرع من السلة ");
            }
        }

        [HttpGet("GetMyDonations")]
        public ActionResult<List<clsModels.MyDonations>> GetMyDonations(int personID)
        {
            var donations = clsBuisnessLayer.GetMyDonations(personID);

            if (donations == null || donations.Count == 0)
                return NotFound(new { message = "لا توجد تبرعات حالياً." });

            var baseUrl = $"{Request.Scheme}://{Request.Host}".TrimEnd('/');

            donations.ForEach(s =>
            {
                if (!string.IsNullOrEmpty(s.SectionImage) && !s.SectionImage.StartsWith("http"))
                {
                    if (!s.SectionImage.StartsWith("/"))
                        s.SectionImage = "/" + s.SectionImage;

                    s.SectionImage = $"{baseUrl}{s.SectionImage}";
                }
            });

            return Ok(donations);
        }

        [HttpPost("AddSlides")]
        public ActionResult<bool> AddSlides([FromForm] clsModels.Slide1 slides,IFormFile image)
        {

            string imageurl = null;

            if (image != null && image.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    image.CopyTo(stream);
                }

                // نخزن الرابط كنسبه مسار داخل السيرفر
                imageurl = $"/images/{uniqueFileName}";
            }

            bool isAdded = clsBuisnessLayer.AddSlides(slides, imageurl);
            if (isAdded)
            {
                return Ok(true);
            }
            else
            {
                return NotFound("من المتوقع ان هناك خطا ما اثناء اضافة الشريحة ");
            }
        }

        [HttpDelete("DeleteSlide")]
        public ActionResult<bool> DeleteSlide(int slideID)
        {
            bool isDeleted = clsBuisnessLayer.DeleteSlide(slideID);
            if (isDeleted)
            {
                return Ok("تم حذف الشريحة بنجاح");
            }
            else
            {
                return NotFound("من المتوقع ان هناك خطا ما اثناء حذف الشريحة ");
            }
        }

        [HttpGet("GetAllSlides")]
        public ActionResult<List<clsModels.Slide1Response>> GetAllSlides()
        {
            var slides = clsBuisnessLayer.GetAllSlides();
            if (slides == null || slides.Count == 0)
                return NotFound("لا يوجد شرائح حاليا");

            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            slides.ForEach(s =>
            {
                  if (!string.IsNullOrEmpty(s.ImageUrl))
                  {
                      // لو الرابط مش فيه http/https نضيف الـ baseUrl
                      if (!s.ImageUrl.StartsWith("http"))
                          s.ImageUrl = $"{baseUrl}{s.ImageUrl}";
                  }
            });

            return Ok(slides);
        }

        [HttpPut("UpdateSlide")]
        public ActionResult<bool> UpdateSlide([FromForm] clsModels.Slide1 slide , IFormFile image , int PersonID)
        {
            string imageurl = null;

            if (image != null && image.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    image.CopyTo(stream);
                }

                // نخزن الرابط كنسبه مسار داخل السيرفر
                imageurl = $"/images/{uniqueFileName}";
            }

            bool isUpdated = clsBuisnessLayer.UpdateSlide(slide , imageurl , PersonID);
            if (isUpdated)
            {
                return Ok("تم تحديث الشريحة بنجاح");
            }
            else
            {
                return NotFound("من المتوقع ان هناك خطا ما اثناء تحديث الشريحة ");
            }
        }

        [HttpPost("AddSlide2")]
        public ActionResult<bool> AddSlide2([FromForm] clsModels.Slide2 slide2, List<IFormFile> images)
        {
            List<string> imageUrls = new List<string>();
            if (images != null && images.Count > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);
                foreach (var image in images)
                {
                    var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        image.CopyTo(stream);
                    }
                    // نخزن الرابط كنسبه مسار داخل السيرفر
                    imageUrls.Add($"/images/{uniqueFileName}");
                }
            }
            bool isAdded = clsBuisnessLayer.IsAdded(slide2, imageUrls);
            if (isAdded)
            {
                return Ok(true);
            }
            else
            {
                return NotFound("من المتوقع ان هناك خطا ما اثناء اضافة الشريحة ");
            }
        }

        [HttpGet("GetSlide2")]
        public ActionResult<List<clsModels.Slide2Response>> GetSlide2()
        {
            var slides2 = clsBuisnessLayer.GetAllSlides2();
            if (slides2 == null || slides2.Count == 0)
                return NotFound("لا يوجد شرائح حاليا");
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            slides2.ForEach(s =>
            {
                for (int i = 0; i < s.Images.Count; i++)
                {
                    if (!string.IsNullOrEmpty(s.Images[i]))
                    {
                        // لو الرابط مش فيه http/https نضيف الـ baseUrl
                        if (!s.Images[i].StartsWith("http"))
                        {
                            s.Images[i] = $"{baseUrl}{s.Images[i]}";
                        }
                    }
                }
            });
            return Ok(slides2);
        }

        [HttpDelete("DeleteSlide2")]
        public ActionResult<bool> DeleteSlide2(int Slide2ID)
        {
            var Del = clsBuisnessLayer.DeleteSlide2(Slide2ID);
            if (Del)
            {
                return Ok("تم ازاله الشريحه بنجاح");
            }
            return BadRequest("يبدو ان هناك خطا ما قد حدث اثناء ازاله الشريحه ");
        }


        // Slide 3
        [HttpPost("AddSlides3")]
        public ActionResult<bool> AddSlides3([FromForm] clsModels.Slide3 slides, IFormFile image)
        {
            string imageurl = null;

            if (image != null && image.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    image.CopyTo(stream);
                }

                imageurl = $"/images/{uniqueFileName}";
            }

            bool isAdded = clsBuisnessLayer.AddSlides3(slides, imageurl);
            if (isAdded)
            {
                return Ok(true);
            }
            else
            {
                return NotFound("من المتوقع ان هناك خطا ما اثناء اضافة الشريحة ");
            }
        }

        [HttpDelete("DeleteSlide3")]
        public ActionResult<bool> DeleteSlide3(int slideID)
        {
            bool isDeleted = clsBuisnessLayer.DeleteSlide3(slideID);
            if (isDeleted)
            {
                return Ok("تم حذف الشريحة بنجاح");
            }
            else
            {
                return NotFound("من المتوقع ان هناك خطا ما اثناء حذف الشريحة ");
            }
        }

        [HttpGet("GetAllSlides3")]
        public ActionResult<List<clsModels.Slide3Response>> GetAllSlides3()
        {
            var slides = clsBuisnessLayer.GetAllSlides3();
            if (slides == null || slides.Count == 0)
                return NotFound("لا يوجد شرائح حاليا");

            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            slides.ForEach(s =>
            {
                if (!string.IsNullOrEmpty(s.ImageUrl))
                {
                    if (!s.ImageUrl.StartsWith("http"))
                        s.ImageUrl = $"{baseUrl}{s.ImageUrl}";
                }
            });

            return Ok(slides);
        }

        // Slide 4
        [HttpPost("AddSlides4")]
        public ActionResult<bool> AddSlides4([FromForm] clsModels.Slide4 slides, IFormFile image)
        {
            string imageurl = null;

            if (image != null && image.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    image.CopyTo(stream);
                }

                imageurl = $"/images/{uniqueFileName}";
            }

            bool isAdded = clsBuisnessLayer.AddSlides4(slides, imageurl);
            if (isAdded)
            {
                return Ok(true);
            }
            else
            {
                return NotFound("من المتوقع ان هناك خطا ما اثناء اضافة الشريحة ");
            }
        }

        [HttpDelete("DeleteSlide4")]
        public ActionResult<bool> DeleteSlide4(int slideID)
        {
            bool isDeleted = clsBuisnessLayer.DeleteSlide4(slideID);
            if (isDeleted)
            {
                return Ok("تم حذف الشريحة بنجاح");
            }
            else
            {
                return NotFound("من المتوقع ان هناك خطا ما اثناء حذف الشريحة ");
            }
        }

        [HttpGet("GetAllSlides4")]
        public ActionResult<List<clsModels.Slide4Response>> GetAllSlides4()
        {
            var slides = clsBuisnessLayer.GetAllSlides4();
            if (slides == null || slides.Count == 0)
                return NotFound("لا يوجد شرائح حاليا");

            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            slides.ForEach(s =>
            {
                if (!string.IsNullOrEmpty(s.ImageUrl))
                {
                    if (!s.ImageUrl.StartsWith("http"))
                        s.ImageUrl = $"{baseUrl}{s.ImageUrl}";
                }
            });

            return Ok(slides);
        }

        // Slide 5
        [HttpPost("AddSlides5")]
        public ActionResult<bool> AddSlides5([FromForm] clsModels.Slide5 slides, IFormFile image)
        {
            string imageurl = null;

            if (image != null && image.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    image.CopyTo(stream);
                }

                imageurl = $"/images/{uniqueFileName}";
            }

            bool isAdded = clsBuisnessLayer.AddSlides5(slides, imageurl);
            if (isAdded)
            {
                return Ok(true);
            }
            else
            {
                return NotFound("من المتوقع ان هناك خطا ما اثناء اضافة الشريحة ");
            }
        }

        [HttpDelete("DeleteSlide5")]
        public ActionResult<bool> DeleteSlide5(int slideID)
        {
            bool isDeleted = clsBuisnessLayer.DeleteSlide5(slideID);
            if (isDeleted)
            {
                return Ok("تم حذف الشريحة بنجاح");
            }
            else
            {
                return NotFound("من المتوقع ان هناك خطا ما اثناء حذف الشريحة ");
            }
        }

        [HttpGet("GetAllSlides5")]
        public ActionResult<List<clsModels.Slide5Response>> GetAllSlides5()
        {
            var slides = clsBuisnessLayer.GetAllSlides5();
            if (slides == null || slides.Count == 0)
                return NotFound("لا يوجد شرائح حاليا");

            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            slides.ForEach(s =>
            {
                if (!string.IsNullOrEmpty(s.ImageUrl))
                {
                    if (!s.ImageUrl.StartsWith("http"))
                        s.ImageUrl = $"{baseUrl}{s.ImageUrl}";
                }
            });

            return Ok(slides);
        }

        // Slide 6
        [HttpPost("AddSlides6")]
        public ActionResult<bool> AddSlides6([FromForm] clsModels.Slide6 slides, IFormFile image)
        {
            string imageurl = null;

            if (image != null && image.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    image.CopyTo(stream);
                }

                imageurl = $"/images/{uniqueFileName}";
            }

            bool isAdded = clsBuisnessLayer.AddSlides6(slides, imageurl);
            if (isAdded)
            {
                return Ok(true);
            }
            else
            {
                return NotFound("من المتوقع ان هناك خطا ما اثناء اضافة الشريحة ");
            }
        }

        [HttpDelete("DeleteSlide6")]
        public ActionResult<bool> DeleteSlide6(int slideID)
        {
            bool isDeleted = clsBuisnessLayer.DeleteSlide6(slideID);
            if (isDeleted)
            {
                return Ok("تم حذف الشريحة بنجاح");
            }
            else
            {
                return NotFound("من المتوقع ان هناك خطا ما اثناء حذف الشريحة ");
            }
        }

        [HttpGet("GetAllSlides6")]
        public ActionResult<List<clsModels.Slide6Response>> GetAllSlides6()
        {
            var slides = clsBuisnessLayer.GetAllSlides6();
            if (slides == null || slides.Count == 0)
                return NotFound("لا يوجد شرائح حاليا");

            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            slides.ForEach(s =>
            {
                if (!string.IsNullOrEmpty(s.ImageUrl))
                {
                    if (!s.ImageUrl.StartsWith("http"))
                        s.ImageUrl = $"{baseUrl}{s.ImageUrl}";
                }
            });

            return Ok(slides);
        }

        [HttpPost("CreatePaymentSession")]
        public ActionResult<PaymentSassion> CreatePaymentSassionStatus([FromBody] PaymentSassionRequest request)
        {
            try
            {
                var session = clsBuisnessLayer.CreatePaymentSession(request);
                return Ok(session);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("UpdatePaymentStatus")]
        public IActionResult UpdatePaymentStatus([FromQuery] string transactionRef, [FromQuery] string status)
        {
            bool updated = clsBuisnessLayer.UpdatePaymentSassionStatus(transactionRef, status);

            if (updated)
                return Ok(new { message = "Status updated successfully" });
            else
                return BadRequest(new { message = "Failed to update payment status" });
        }

        [HttpPost("ConfirmDonation")]
        public ActionResult<DonationPaymentResponse> ConfirmDonation([FromBody]DonationPaymentRequest request)
        {
  
            var result = clsBuisnessLayer.ConfirmOrder(request);

            if (result != null)
                return Ok(result);
            else
                return NotFound(new DonationPaymentResponse
                {
                    Success = false,
                    Message = "حدث خطأ أثناء تأكيد التبرع."
                });
        }

    }
}

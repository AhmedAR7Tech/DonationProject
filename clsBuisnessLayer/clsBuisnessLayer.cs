using ClsDataAccess;
using ClsModel;
using System;
using System.Net.Http.Headers;
using static ClsModel.clsModels;


namespace ClsBuisnessLayer
{
    public class clsBuisnessLayer
    {
        static public clsModels.LoginResponse LoginUser(clsModels.Login login)
        {
           clsModels.LoginResponse login1 = clsDataAccess.LoginUser(login);

            if (login1 != null)
            {
                return login1;
            }
            else
            {
                return null;
            }
        }

        static public clsModels.LoginResponse VerifyOTP(clsModels.VerifyOTP verifyOTP)
        {
            clsModels.LoginResponse login1 = clsDataAccess.VerifyOTP(verifyOTP);

            if (login1 != null)
            {
                if (login1.Name == "Admin5153920")
                {
                    login1.Role = "Admin";
                }
                else
                {
                    login1.Role = "User";
                }
                return login1;
            }
            else
            {
                return null;
            }
        }

        static public bool AddSections(clsModels.AddSections addSections , string ImageUrl)
        {
            bool isAdded = clsDataAccess.AddSections(addSections , ImageUrl);
            if (isAdded)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        static public bool DeleteSection(int sectionID)
        {
            bool isDeleted = clsDataAccess.DeletSection(sectionID);
            if (isDeleted)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        static public List<clsModels.GetAllSections> GetAllSections()
        {
            List<clsModels.GetAllSections> sections = clsDataAccess.GetAllSections();
            if(sections != null && sections.Count > 0)
            {
                return sections;
            }
            else
            {
                return new List<clsModels.GetAllSections>();
            }   
        }

        static public bool AddToDonationCart(clsModels.DonationCart donationCart)
        {
            bool isAdded = clsDataAccess.InserDataInCart(donationCart);

            if (isAdded)
            {
                // نحسب التوتال الجديد بعد الإضافة
                Decimal totalAmount = clsDataAccess.GetTotalAmount(donationCart.PersonID);
                donationCart.TotalAmount = totalAmount;
                return true;
            }
            else
            {
                return false;
            }
        }

        static public List<clsModels.GetAllDonatonCart> GetDonationCart(int personID)
        {
            List<clsModels.GetAllDonatonCart> donationCart = clsDataAccess.GetAllDonationCart(personID);
            if (donationCart != null)
            {
                return donationCart;
            }
            else
            {
                return null;
            }
        }

        static public bool RemoveFromDonationCart(clsModels.RemoveDonationCart remove)
        {
            bool isRemoved = clsDataAccess.DeleteDonationCartItem(remove);
            if (isRemoved)
            {
                // نحسب التوتال الجديد بعد الحذف
                Decimal totalAmount = clsDataAccess.GetTotalAmount(remove.PersonID);
                return true;
            }
            else
            {
                return false;
            }
        }

        static public List<clsModels.MyDonations> GetMyDonations(int personID)
        {
            List<clsModels.MyDonations> myDonations = clsDataAccess.GetMyDonations(personID);
            if (myDonations != null)
            {
                return myDonations;
            }
            else
            {
                return null;
            }
        }

        static public bool AddSlides(clsModels.Slide1 slides , string ImageUrl)
        {
            bool isAdded = clsDataAccess.AddSlide(slides , ImageUrl);
            if (isAdded)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        static public bool DeleteSlide(int slideID)
        {
            bool isDeleted = clsDataAccess.DeleteSlide(slideID);
            if (isDeleted)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        static public List<clsModels.Slide1Response> GetAllSlides()
        {
            List<clsModels.Slide1Response> slides = clsDataAccess.GetAllSlides();
            if (slides != null && slides.Count > 0)
            {
                return slides;
            }
            else
            {
                return new List<clsModels.Slide1Response>();
            }
        }

        static public bool UpdateSlide(clsModels.Slide1 slide , string Image , int PersonID)
        {
            bool isUpdated = clsDataAccess.UpdateSlide(slide , Image , PersonID);
            if (isUpdated)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        // Slide 3
        static public bool AddSlides3(clsModels.Slide3 slides, string ImageUrl)
        {
            bool isAdded = clsDataAccess.AddSlide3(slides, ImageUrl);
            if (isAdded)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        static public bool DeleteSlide3(int slideID)
        {
            bool isDeleted = clsDataAccess.DeleteSlide3(slideID);
            if (isDeleted)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        static public List<clsModels.Slide3Response> GetAllSlides3()
        {
            List<clsModels.Slide3Response> slides = clsDataAccess.GetAllSlides3();
            if (slides != null && slides.Count > 0)
            {
                return slides;
            }
            else
            {
                return new List<clsModels.Slide3Response>();
            }
        }

        // Slide 4
        static public bool AddSlides4(clsModels.Slide4 slides, string ImageUrl)
        {
            bool isAdded = clsDataAccess.AddSlide4(slides, ImageUrl);
            if (isAdded)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        static public bool DeleteSlide4(int slideID)
        {
            bool isDeleted = clsDataAccess.DeleteSlide4(slideID);
            if (isDeleted)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        static public List<clsModels.Slide4Response> GetAllSlides4()
        {
            List<clsModels.Slide4Response> slides = clsDataAccess.GetAllSlides4();
            if (slides != null && slides.Count > 0)
            {
                return slides;
            }
            else
            {
                return new List<clsModels.Slide4Response>();
            }
        }

        // Slide 5
        static public bool AddSlides5(clsModels.Slide5 slides, string ImageUrl)
        {
            bool isAdded = clsDataAccess.AddSlide5(slides, ImageUrl);
            if (isAdded)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        static public bool DeleteSlide5(int slideID)
        {
            bool isDeleted = clsDataAccess.DeleteSlide5(slideID);
            if (isDeleted)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        static public List<clsModels.Slide5Response> GetAllSlides5()
        {
            List<clsModels.Slide5Response> slides = clsDataAccess.GetAllSlides5();
            if (slides != null && slides.Count > 0)
            {
                return slides;
            }
            else
            {
                return new List<clsModels.Slide5Response>();
            }
        }

        // Slide 6
        static public bool AddSlides6(clsModels.Slide6 slides, string ImageUrl)
        {
            bool isAdded = clsDataAccess.AddSlide6(slides, ImageUrl);
            if (isAdded)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        static public bool DeleteSlide6(int slideID)
        {
            bool isDeleted = clsDataAccess.DeleteSlide6(slideID);
            if (isDeleted)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        static public List<clsModels.Slide6Response> GetAllSlides6()
        {
            List<clsModels.Slide6Response> slides = clsDataAccess.GetAllSlides6();
            if (slides != null && slides.Count > 0)
            {
                return slides;
            }
            else
            {
                return new List<clsModels.Slide6Response>();
            }
        }

        static public bool IsAdded(clsModels.Slide2 slide2 , List<string> Images)
        {
            bool isAdded = clsDataAccess.AddSlide2(slide2 , Images);
            if (isAdded)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        static public List<clsModels.Slide2Response> GetAllSlides2()
        {
            List<clsModels.Slide2Response> slides2 = clsDataAccess.GetAllSlides2();
            if (slides2 != null && slides2.Count > 0)
            {
                return slides2;
            }
            else
            {
                return new List<clsModels.Slide2Response>();
            }
        }

        static public bool DeleteSlide2(int Slide2ID)
        {
            var Del = clsDataAccess.DeleteSlide2(Slide2ID);
            if(Del != null)
            {
                return true;
            }
            return false;
        }

        static public clsModels.PaymentSassion CreatePaymentSession(clsModels.PaymentSassionRequest request)
        {
            string transactionRef = "TXN-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

            // 2️⃣ نجهز الموديل
            PaymentSassion session = new PaymentSassion
            {
                TransactionReference = transactionRef,
                PersonID = request.PersonID,
                Amount = request.Amount,    
                PaymentMethod = request.PaymentMethod,
                Status = "Pending",
                CreatedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddMinutes(15)
            };

            // 3️⃣ نحفظه في قاعدة البيانات
            bool isSaved = clsDataAccess.CreatePaymentSession(session);

            if (!isSaved)
                throw new Exception("Failed to create payment session.");

            // 4️⃣ نرجع الجلسة
            return session;
        }

        public static bool UpdatePaymentSassionStatus(string transactionRef, string newStatus)
        {
            return clsDataAccess.UpdatePaymentSessionStatus(transactionRef, newStatus);
        }

        public static DonationPaymentResponse ConfirmOrder(DonationPaymentRequest request)
        {
            // 1️⃣ نجيب آخر سيشن للدفع الخاصة بالشخص
            var session = clsDataAccess.GetPaymentSessionByPersonID(request.PersonID);

            // 2️⃣ نتأكد إن فيه جلسة دفع ناجحة
            if (session == null || session.Status != "Success")
            {
                return new DonationPaymentResponse
                {
                    Success = false,
                    Message = "Payment not verified yet."
                };
            }

            // 3️⃣ ننفذ عملية التبرع فعلاً
            var result = clsDataAccess.ConfirmDonation(request);

            if (result.Success)
            {
                // 4️⃣ نحدث حالة الدفع إلى Completed
                clsDataAccess.UpdatePaymentSessionStatus(session.TransactionReference, "Completed");
            }

            // 5️⃣ نرجع النتيجة
            return result;
        }


    }
}

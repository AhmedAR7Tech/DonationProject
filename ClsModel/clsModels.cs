using System.Text.Json.Serialization;

namespace ClsModel
{
    public class clsModels
    {
        public class Login
        {
            public string Name { get; set; }    
            public string E_Mail { get; set; }
            public string PhoneNumber { get; set; }
        }

        public class LoginResponse : Login
        {   
            public int PersonID { get; set; }

            public string Role { get; set; }   
            
            public int IsVerified { get; set; }

            public string Token { get; set; } // ✅ مضاف للتوكين بعد النجاح
        }

        public class VerifyOTP
        {
            public int PersonID {get; set;}

            public string OTP { get; set;} 
        }

        public class AddSections
        {
            public string Name { get; set; }
            public string TargetAmount { get; set; }
            public DateTime Durations { get; set; }
        }

        public class GetAllSections
        {
            public int SectionID { get; set; }
            public string Name { get; set; }
            public string ImageUrl { get; set; }
            public Decimal TargetAmount { get; set; }
            public DateTime Durations { get; set; }
            public decimal CollectedAmount { get; set; }
            public int DonorsCount { get; set; }    
        }   

        public class DonationCart
        {
            public int PersonID { get; set; }
            public int SectionID { get; set; }
            public Decimal Amount { get; set; }
     
            [System.Text.Json.Serialization.JsonIgnore]
            public decimal TotalAmount { get; set; }   
        }

        public class GetAllDonatonCart
        {
            public int PersonID { get; set; }
            public int SectionID { get; set; }
            public Decimal Amount { get; set; }
            public decimal TotalAmount { get; set; }    

            public string SectionName { get; set; }

            public string SectionImage { get; set; }
        }

        public class MyDonations
        {
            public string SectionName { get; set; }
            public string SectionImage { get; set; }
            public decimal Amount { get; set; }
            public DateTime DonationDate { get; set; }
        }

        public class RemoveDonationCart
        {
            public int PersonID { get; set; }
            public int SectionID { get; set; }
        }

        public class Slide1
        {
            public string Title { get; set; }
            public string Description { get; set; }

        }

        public class Slide1Response : Slide1
        {
            public int SlideID { get; set; }

            public string ImageUrl { get; set; }
        }

        public class Slide3
        {
            public string Title { get; set; }
            public string Description { get; set; }

        }

        public class Slide3Response : Slide3
        {
            public int Slide3ID { get; set; }

            public string ImageUrl { get; set; }
        }

        public class Slide4
        {
            public string Title { get; set; }
            public string Description { get; set; }

        }

        public class Slide4Response : Slide4
        {
            public int Slide4ID { get; set; }

            public string ImageUrl { get; set; }
        }

        public class Slide5
        {
            public string Title { get; set; }
            public string Description { get; set; }

        }

        public class Slide5Response : Slide5
        {
            public int Slide5ID { get; set; }

            public string ImageUrl { get; set; }
        }

        public class Slide6
        {
            public string Title { get; set; }
            public string Description { get; set; }

        }

        public class Slide6Response : Slide6
        {
            public int Slide6ID { get; set; }

            public string ImageUrl { get; set; }
        }

        public class Slide2
        {
            public string Title { get; set; }
            public string Description { get; set; }
        }

        public class Slide2Response : Slide2
        {
            public int Slide2ID { get; set; }
            public List<string>Images { get; set; } = new List<string>();
        }

        public class PaymentSassion
        {
            public string TransactionReference { get; set; }
            public int PersonID { get; set; }
            public decimal Amount { get; set; }
            public string PaymentMethod { get; set; }
            public string Status { get; set; }     // Pending / Success / Failed
            public DateTime CreatedAt { get; set; }
            public DateTime? ExpiresAt { get; set; }
        }

        public class PaymentSassionRequest
        {
            public int PersonID { get; set; }
            public decimal Amount { get; set; }
            public string PaymentMethod { get; set; }
        }

        public class DonationPaymentRequest
        {
            public int PersonID { get; set; }
        }

        public class DonationPaymentResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; }
        }

    }
}

using ClsModel;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Numerics;
using System.Text;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using static ClsModel.clsModels;
using static System.Collections.Specialized.BitVector32;
using static System.Net.WebRequestMethods;

namespace ClsDataAccess
{
    public class clsDataAccess
    {
        static public string ConnectionString = "Server=db30649.public.databaseasp.net;Database=db30649;User Id=db30649;Password=nD?8G@6oc9R+;Encrypt=False; MultipleActiveResultSets=True;";

        private static bool SendEmailOTP(string email, string otp)
        {
            try
            {
                string fromEmail = "Alhadeannabwe@gmail.com"; // بريدك
                string appPassword = "rnwt krez xoxv cxre\r\n";   // App Password من Gmail

                MailMessage message = new MailMessage();
                message.From = new MailAddress(fromEmail);
                message.To.Add(email);
                message.Subject = "رمز التحقق OTP";
                message.Body = $"رمز التحقق الخاص بك هو: {otp} ✅";

                SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
                smtp.Credentials = new NetworkCredential(fromEmail, appPassword);
                smtp.EnableSsl = true;
                smtp.Send(message);

                Console.WriteLine("تم إرسال OTP بنجاح على البريد الإلكتروني ✅");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("خطأ في إرسال OTP: " + ex.Message);
                return false;
            }
        }

        static public clsModels.LoginResponse LoginUser(clsModels.Login login)
        {
            SqlConnection Connection = new SqlConnection(ConnectionString);
            clsModels.LoginResponse user = null;

            string OTP = new Random().Next(100000, 999999).ToString();
            DateTime OTP_Expiry = DateTime.Now.AddMinutes(5);

            string Query = @"insert into Person (Name , PhoneNumber , Role ,E_mail  , OTP , CodeExpiresAt) values 
                    (@Name , @PhoneNumber , @Role , @E_mail , @VerificationCode , @CodeExpiresAt);
                    Select Cast (Scope_Identity() As int);";

            SqlCommand Command = new SqlCommand(Query, Connection);
            Command.Parameters.AddWithValue("@Name", login.Name);
            Command.Parameters.AddWithValue("@PhoneNumber", login.PhoneNumber);
            Command.Parameters.AddWithValue("@E_mail", login.E_Mail);
            Command.Parameters.AddWithValue("@Role",login.Name == "Admin5153920" ? "Admin" : "User");
            Command.Parameters.AddWithValue("@VerificationCode", OTP);
            Command.Parameters.AddWithValue("@CodeExpiresAt", OTP_Expiry);

            try
            {
                Connection.Open();

                string QueryCheck = @"SELECT PersonID, Name, E_mail, Role, IsVerified 
                              FROM Person WHERE PhoneNumber = @PhoneNumber AND E_mail = @E_mail;";
                SqlCommand CommandCheck = new SqlCommand(QueryCheck, Connection);
                CommandCheck.Parameters.AddWithValue("@PhoneNumber", login.PhoneNumber);
                CommandCheck.Parameters.AddWithValue("@E_mail", login.E_Mail);
                SqlDataReader Reader = CommandCheck.ExecuteReader();

                if (Reader.Read())
                {
                    user = new clsModels.LoginResponse();
                    user.PersonID = Convert.ToInt32(Reader["PersonID"]);
                    user.Name = Reader["Name"].ToString();
                    user.E_Mail = Reader["E_mail"].ToString();
                    user.PhoneNumber = login.PhoneNumber;
                    user.Role = Reader["Role"].ToString();
                    user.IsVerified = Convert.ToByte(Reader["IsVerified"]);
                    Reader.Close();
                    return user;
                }
                Reader.Close();

                int personID = Convert.ToInt32(Command.ExecuteScalar());

                user = new clsModels.LoginResponse();
                user.PersonID = personID;
                user.Name = login.Name;
                user.E_Mail = login.E_Mail;
                user.PhoneNumber = login.PhoneNumber;
                user.Role = "User";
                user.IsVerified = 0;

                bool otpSent = SendEmailOTP(login.E_Mail, OTP);
                if (!otpSent)
                    Console.WriteLine("فشل إرسال OTP على البريد الإلكتروني.");


            }
            catch (Exception ex)
            {
                Console.WriteLine("Error is : " + ex.Message);
            }
            finally
            {
                Connection.Close();
            }

            return user;
        }

        static public clsModels.LoginResponse VerifyOTP(clsModels.VerifyOTP verifyOTP)
        {
            SqlConnection Connection = new SqlConnection(ConnectionString);
            clsModels.LoginResponse user = new clsModels.LoginResponse();
            bool isVerified = false;
            string Query = @"SELECT OTP, CodeExpiresAt FROM Person WHERE PersonID = @PersonID;";
            SqlCommand Command = new SqlCommand(Query, Connection);
            Command.Parameters.AddWithValue("@PersonID", verifyOTP.PersonID);
            try
            {
                Connection.Open();
                SqlDataReader Reader = Command.ExecuteReader();
                if (Reader.Read())
                {
                    string storedOTP = Reader["OTP"].ToString();
                    DateTime codeExpiresAt = Convert.ToDateTime(Reader["CodeExpiresAt"]);
                    Reader.Close();
                    if (storedOTP == verifyOTP.OTP && DateTime.Now <= codeExpiresAt)
                    {
                        string UpdateQuery = @"UPDATE Person SET IsVerified = 1 , OTP = null WHERE PersonID = @PersonID;";
                        SqlCommand UpdateCommand = new SqlCommand(UpdateQuery, Connection);
                        UpdateCommand.Parameters.AddWithValue("@PersonID", verifyOTP.PersonID);
                        UpdateCommand.ExecuteNonQuery();
                        isVerified = true;

                        if (isVerified)
                        {
                            string queryGetUser = @"SELECT PersonID, Name, E_mail, PhoneNumber, Role, IsVerified 
                                              FROM Person WHERE PersonID = @PersonID;";

                            SqlCommand Commandplus = new SqlCommand(queryGetUser, Connection);
                            Commandplus.Parameters.AddWithValue("@PersonID", verifyOTP.PersonID);
                            SqlDataReader Readerplus = Commandplus.ExecuteReader();
                            if (Readerplus.Read())
                            {
                                user.PersonID = Convert.ToInt32(Readerplus["PersonID"]);
                                user.Name = Readerplus["Name"].ToString();
                                user.E_Mail = Readerplus["E_mail"].ToString();
                                user.PhoneNumber = Readerplus["PhoneNumber"].ToString();
                                user.Role = Readerplus["Role"].ToString();
                                user.IsVerified = Convert.ToByte(Readerplus["IsVerified"]);
                                Readerplus.Close();
                                return user;
                            }
                        }
                    
                    }
                }
                else
                {
                    Reader.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error is : " + ex.Message);
            }
            finally
            {
                Connection.Close();
            }
            return user;
        }

        static public List<clsModels.GetAllPersons> GetAllPersons()
        {
            SqlConnection Connection = new SqlConnection(ConnectionString);

            List<clsModels.GetAllPersons> personlist = new List<GetAllPersons>();
            clsModels.GetAllPersons person = null;

            string Query = @"select PersonID , Name , E_Mail , PhoneNumber , Role , IsVerified from Person;";

            SqlCommand Command = new SqlCommand(Query, Connection);
            try
            {
                Connection.Open();
                SqlDataReader reader = Command.ExecuteReader();
                while(reader.Read())
                {
                    person = new clsModels.GetAllPersons();
                    {
                        person.PersonID = Convert.ToInt32(reader["PersonID"]);
                        person.Name = reader["Name"].ToString();
                        person.E_Mail = reader["E_Mail"].ToString();
                        person.PhoneNumber = reader["PhoneNumber"].ToString();
                        person.Role = reader["Role"].ToString();
                        person.IsVerified = Convert.ToInt32(reader["IsVerified"]);
                    }
                    personlist.Add(person);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error is : " + ex.Message);
            }
            finally
            {
                Connection.Close();
            }
            return personlist;
        }

        static public bool ChangeRoolByAdmin(int PersonID , string Role)
        {
            SqlConnection Connection = new SqlConnection(ConnectionString);

            bool IsChanged = false;

            string Query = @"Update Person Set Role = @Role Where PersonID = @PersonID";

            SqlCommand Command = new SqlCommand(Query, Connection);
            Command.Parameters.AddWithValue("@PersonID", PersonID);
            Command.Parameters.AddWithValue("@Role", Role);
            try
            {
                Connection.Open();
                int rowsAffected = Command.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    IsChanged = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error is : " + ex.Message);
            }
            finally
            {
                Connection.Close();
            }
            return IsChanged;
        }

        static public bool AddSections(clsModels.AddSections addSections , string ImageUrl)
        {
            SqlConnection Connection = new SqlConnection(ConnectionString);
            bool isAdded = false;
            string Query = @"insert into Section (Name , ImageUrl , TargetAmount , Durations ) values (@Name , @ImageUrl , @TargetAmount , @Durations);";
            SqlCommand Command = new SqlCommand(Query, Connection);
            Command.Parameters.AddWithValue("@Name", addSections.Name);
            Command.Parameters.AddWithValue("@TargetAmount",Convert.ToDecimal(addSections.TargetAmount));
            Command.Parameters.AddWithValue("@Durations", addSections.Durations);

            if (ImageUrl != null && ImageUrl.Length > 0)
            {
                Command.Parameters.AddWithValue("@ImageUrl",ImageUrl);
            }
            else
            {
                Command.Parameters.AddWithValue("@ImageUrl", DBNull.Value);
            }

            try
            {
                Connection.Open();
                int rowsAffected = Command.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    isAdded = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error is : " + ex.Message);
            }
            finally
            {
                Connection.Close();
            }
            return isAdded;
        }

        static public List<clsModels.GetAllSections> GetAllSections()
        {
            SqlConnection Connection = new SqlConnection(ConnectionString);
            List<clsModels.GetAllSections> sections = new List<clsModels.GetAllSections>();
            clsModels.GetAllSections section = null;
            string Query = @"select SectionID , Name , ImageUrl , TargetAmount , Durations , CollectedAmount ,DonorsCount from Section;";
            SqlCommand Command = new SqlCommand(Query, Connection);
            try
            {
                Connection.Open();
                SqlDataReader reader = Command.ExecuteReader();
                while (reader.Read())
                {
                    section = new clsModels.GetAllSections();
                    {
                        section.SectionID = Convert.ToInt32(reader["SectionID"]);
                        section.Name = reader["Name"].ToString();
                        section.ImageUrl = reader["ImageUrl"].ToString();
                        section.TargetAmount = Convert.ToDecimal(reader["TargetAmount"]);
                        section.Durations = Convert.ToDateTime(reader["Durations"]);
                        section.CollectedAmount = Convert.ToDecimal(reader["CollectedAmount"]);
                        section.DonorsCount = Convert.ToInt32(reader["DonorsCount"]);
                    }
                    sections.Add(section);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error is : " + ex.Message);
            }
            finally
            {
                Connection.Close();
            }
            return sections;
        }

        static public bool DeletSection(int sectionID)
        {
            SqlConnection Connection = new SqlConnection(ConnectionString);
            bool isDeleted = false;
            string Query = @"delete from Section where SectionID = @SectionID;";
            SqlCommand Command = new SqlCommand(Query, Connection);
            Command.Parameters.AddWithValue("@SectionID", sectionID);
            try
            {
                Connection.Open();
                int rowsAffected = Command.ExecuteNonQuery();
                if (rowsAffected > 0)
                {

                    isDeleted = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error is : " + ex.Message);
            }
            finally
            {
                Connection.Close();
            }
            return isDeleted;
        }

        static public bool InserDataInCart(clsModels.DonationCart donationCart)
        {
            SqlConnection Connection = new SqlConnection(ConnectionString);
            bool isAdded = false;
            string Query = @"insert into DonationCart (PersonID , SectionID , Amount ) values (@PersonID , @SectionID , @Amount );";
            SqlCommand Command = new SqlCommand(Query, Connection);
            Command.Parameters.AddWithValue("@PersonID", donationCart.PersonID);
            Command.Parameters.AddWithValue("@SectionID", donationCart.SectionID);
            Command.Parameters.AddWithValue("@Amount", donationCart.Amount);
            try
            {
                Connection.Open();
                string checkQuery = @"Select TargetAmount , CollectedAmount From Section Where SectionID = @SectionID";
                SqlCommand checkCommand = new SqlCommand(checkQuery, Connection);
                checkCommand.Parameters.AddWithValue("@SectionID", donationCart.SectionID);
                SqlDataReader reader = checkCommand.ExecuteReader();
                if (reader.Read())
                {
                    decimal targetAmount = Convert.ToDecimal(reader["TargetAmount"]);
                    decimal collectedAmount = Convert.ToDecimal(reader["CollectedAmount"]);
                    reader.Close();
                    if (collectedAmount + donationCart.Amount > targetAmount)
                    {
                        decimal MaxDonation = targetAmount - collectedAmount;
                        throw new InvalidOperationException($"لا يمكن إضافة التبرع، المبلغ يتجاوز الهدف المحدد للقسم يمكنك التبرع بمبلغ اذا كنت تريد ان تحقق الهدف {MaxDonation} .");
                    }
                }
                else
                {
                    reader.Close();
                    Console.WriteLine("القسم غير موجود.");
                    return false;
                }
                int rowsAffected = Command.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    decimal TotalAmount = GetTotalAmount(donationCart.PersonID);
                    string updateQuery = @"update DonationCart set TotalAmount = @TotalAmount where PersonID = @PersonID;";
                    SqlCommand updateCommand = new SqlCommand(updateQuery, Connection);
                    updateCommand.Parameters.AddWithValue("@TotalAmount", TotalAmount);
                    updateCommand.Parameters.AddWithValue("@PersonID", donationCart.PersonID);
                    updateCommand.ExecuteNonQuery();

                    isAdded = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error is : " + ex.Message);
                throw;
            }
            finally
            {
                Connection.Close();
            }
            return isAdded;
        }

        static public Decimal GetTotalAmount(int personID)
        {
            SqlConnection Connection = new SqlConnection(ConnectionString);
            Decimal totalAmount = 0;
            string Query = @"select SUM(Amount) as TotalAmount from DonationCart where PersonID = @PersonID And SectionID IS not null;";
            SqlCommand Command = new SqlCommand(Query, Connection);
            Command.Parameters.AddWithValue("@PersonID", personID);
            try
            {
                Connection.Open();
                object result = Command.ExecuteScalar();
                if (result != DBNull.Value)
                {
                    totalAmount = Convert.ToDecimal(result);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error is : " + ex.Message);
            }
            finally
            {
                Connection.Close();
            }
            return totalAmount;
        }

        static public List<clsModels.GetAllDonatonCart> GetAllDonationCart(int personID)
        {
            SqlConnection Connection = new SqlConnection(ConnectionString);
            List<clsModels.GetAllDonatonCart> donationCarts = new List<clsModels.GetAllDonatonCart>();
            clsModels.GetAllDonatonCart donationCart = null;
            string Query = @"select D.PersonID , D.SectionID  , S.Name , S.ImageUrl , D.Amount  , TotalAmount 
                            from DonationCart D join Section S ON D.SectionID = S.SectionID 
                                Where PersonID = @PersonID AND D.SectionID IS NOT NULL;";
            SqlCommand Command = new SqlCommand(Query, Connection);
            Command.Parameters.AddWithValue("@PersonID", personID);
            try
            {
                Connection.Open();
                SqlDataReader reader = Command.ExecuteReader();
                decimal totalAmount = GetTotalAmount(personID);

                while (reader.Read())
                {
                    donationCart = new clsModels.GetAllDonatonCart();
                    {
                        decimal TotalAmount = GetTotalAmount(personID);
                        donationCart.PersonID = Convert.ToInt32(reader["PersonID"]);
                        donationCart.SectionID = Convert.ToInt32(reader["SectionID"]);
                        donationCart.Amount = Convert.ToDecimal(reader["Amount"]);
                        if (reader["TotalAmount"] != DBNull.Value)
                        {
                            donationCart.TotalAmount = totalAmount;
                        }
                        else
                        {
                            donationCart.TotalAmount = 0; // أو أي قيمة افتراضية تحبها
                        }
                        donationCart.SectionName = reader["Name"].ToString();
                        donationCart.SectionImage = reader["ImageUrl"].ToString();
                    }
                    donationCarts.Add(donationCart);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error is : " + ex.Message);
            }
            finally
            {
                Connection.Close();
            }
            return donationCarts;
        }

        static public bool DeleteDonationCartItem(clsModels.RemoveDonationCart Remove)
        {
            SqlConnection Connection = new SqlConnection(ConnectionString);
            bool isDeleted = false;
            string Query = @"delete from DonationCart where PersonID = @PersonID And SectionID = @SectionID;";
            SqlCommand Command = new SqlCommand(Query, Connection);
            Command.Parameters.AddWithValue("@PersonID", Remove.PersonID);
            Command.Parameters.AddWithValue("@SectionID", Remove.SectionID);
            try
            {
                Connection.Open();
                int rowsAffected = Command.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    decimal TotalAmount = GetTotalAmount(Remove.PersonID);
                    string updateQuery = @"update DonationCart set TotalAmount = @TotalAmount where PersonID = @PersonID;";
                    SqlCommand updateCommand = new SqlCommand(updateQuery, Connection);
                    updateCommand.Parameters.AddWithValue("@TotalAmount", TotalAmount);
                    updateCommand.Parameters.AddWithValue("@PersonID", Remove.PersonID);
                    updateCommand.ExecuteNonQuery();
                    isDeleted = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error is : " + ex.Message);
            }
            finally
            {
                Connection.Close();
            }
            return isDeleted;
        }

        static public List<clsModels.MyDonations> GetMyDonations(int personID)
        {
            SqlConnection Connection = new SqlConnection(ConnectionString);
            List<clsModels.MyDonations> myDonations = new List<clsModels.MyDonations>();
            clsModels.MyDonations donation = null;
            string Query = @"select SectionName , SectionImage , Amount , DonationDate from Donation Where PersonID = @PersonID;";
            SqlCommand Command = new SqlCommand(Query, Connection);
            Command.Parameters.AddWithValue("@PersonID", personID);
            try
            {
                Connection.Open();
                SqlDataReader reader = Command.ExecuteReader();
                while (reader.Read())
                {
                    donation = new clsModels.MyDonations();
                    {
                        donation.SectionName = reader["SectionName"].ToString();
                        donation.SectionImage = reader["SectionImage"].ToString();
                        donation.Amount = Convert.ToDecimal(reader["Amount"]);
                        donation.DonationDate = Convert.ToDateTime(reader["DonationDate"]);
                    }
                    myDonations.Add(donation);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error is : " + ex.Message);
            }
            finally
            {
                Connection.Close();
            }
            return myDonations;
        }

        static public bool AddSlide(clsModels.Slide1 slide , string ImageUrl)
        {
            SqlConnection Connection = new SqlConnection(ConnectionString);
            bool isAdded = false;
            string Query = @"insert into Slide1 (Title , Description , ImageUrl ) values (@Title , @Description , @ImageUrl );";
            SqlCommand Command = new SqlCommand(Query, Connection);
            Command.Parameters.AddWithValue("@Title", slide.Title);
            Command.Parameters.AddWithValue("@Description", slide.Description);
            Command.Parameters.AddWithValue("@ImageUrl", ImageUrl);
            try
            {
                Connection.Open();
                int rowsAffected = Command.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    isAdded = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error is : " + ex.Message);
            }
            finally
            {
                Connection.Close();
            }
            return isAdded;
        }

        static public List<clsModels.Slide1Response> GetAllSlides()
        {
            SqlConnection Connection = new SqlConnection(ConnectionString);
            List<clsModels.Slide1Response> slides = new List<clsModels.Slide1Response>();
            clsModels.Slide1Response slide = null;
            string Query = @"select Slide1 , Title , Description , ImageUrl from Slide1;";
            SqlCommand Command = new SqlCommand(Query, Connection);
            try
            {
                Connection.Open();
                SqlDataReader reader = Command.ExecuteReader();
                while (reader.Read())
                {
                    slide = new clsModels.Slide1Response();
                    {
                        slide.SlideID = Convert.ToInt32(reader["Slide1"]);
                        slide.Title = reader["Title"].ToString();
                        slide.Description = reader["Description"].ToString();
                        slide.ImageUrl = reader["ImageUrl"].ToString();
                    }
                    slides.Add(slide);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error is : " + ex.Message);
            }
            finally
            {
                Connection.Close();
            }
            return slides;
        }

        static public bool DeleteSlide(int slideID)
        {
            SqlConnection Connection = new SqlConnection(ConnectionString);
            bool isDeleted = false;
            string Query = @"delete from Slide1 where Slide1 = @Slide1;";
            SqlCommand Command = new SqlCommand(Query, Connection);
            Command.Parameters.AddWithValue("@Slide1", slideID);
            try
            {
                Connection.Open();
                int rowsAffected = Command.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    isDeleted = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error is : " + ex.Message);
            }
            finally
            {
                Connection.Close();
            }
            return isDeleted;
        }

        static public bool AddSlide3(clsModels.Slide3 slide, string ImageUrl)
        {
            SqlConnection Connection = new SqlConnection(ConnectionString);
            bool isAdded = false;
            string Query = @"insert into Slide3 (Title , Description , ImageUrl ) values (@Title , @Description , @ImageUrl );";
            SqlCommand Command = new SqlCommand(Query, Connection);
            Command.Parameters.AddWithValue("@Title", slide.Title);
            Command.Parameters.AddWithValue("@Description", slide.Description);
            Command.Parameters.AddWithValue("@ImageUrl", ImageUrl);
            try
            {
                Connection.Open();
                int rowsAffected = Command.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    isAdded = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error is : " + ex.Message);
            }
            finally
            {
                Connection.Close();
            }
            return isAdded;
        }

        static public List<clsModels.Slide3Response> GetAllSlides3()
        {
            SqlConnection Connection = new SqlConnection(ConnectionString);
            List<clsModels.Slide3Response> slides = new List<clsModels.Slide3Response>();
            clsModels.Slide3Response slide = null;
            string Query = @"select Slide3ID , Title , Description , ImageUrl from Slide3;";
            SqlCommand Command = new SqlCommand(Query, Connection);
            try
            {
                Connection.Open();
                SqlDataReader reader = Command.ExecuteReader();
                while (reader.Read())
                {
                    slide = new clsModels.Slide3Response();
                    {
                        slide.Slide3ID = Convert.ToInt32(reader["Slide3ID"]);
                        slide.Title = reader["Title"].ToString();
                        slide.Description = reader["Description"].ToString();
                        slide.ImageUrl = reader["ImageUrl"].ToString();
                    }
                    slides.Add(slide);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error is : " + ex.Message);
            }
            finally
            {
                Connection.Close();
            }
            return slides;
        }

        static public bool DeleteSlide3(int slide3ID)
        {
            SqlConnection Connection = new SqlConnection(ConnectionString);
            bool isDeleted = false;
            string Query = @"delete from Slide3 where Slide3ID = @Slide1;";
            SqlCommand Command = new SqlCommand(Query, Connection);
            Command.Parameters.AddWithValue("@Slide1", slide3ID);
            try
            {
                Connection.Open();
                int rowsAffected = Command.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    isDeleted = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error is : " + ex.Message);
            }
            finally
            {
                Connection.Close();
            }
            return isDeleted;
        }
        //Slide 3

        // Slide 4
        static public bool AddSlide4(clsModels.Slide4 slide, string ImageUrl)
        {
            SqlConnection Connection = new SqlConnection(ConnectionString);
            bool isAdded = false;
            string Query = @"insert into Slide4 (Title, Description, ImageUrl) values (@Title, @Description, @ImageUrl);";
            SqlCommand Command = new SqlCommand(Query, Connection);
            Command.Parameters.AddWithValue("@Title", slide.Title);
            Command.Parameters.AddWithValue("@Description", slide.Description);
            Command.Parameters.AddWithValue("@ImageUrl", ImageUrl);
            try
            {
                Connection.Open();
                int rowsAffected = Command.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    isAdded = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error is : " + ex.Message);
            }
            finally
            {
                Connection.Close();
            }
            return isAdded;
        }

        static public List<clsModels.Slide4Response> GetAllSlides4()
        {
            SqlConnection Connection = new SqlConnection(ConnectionString);
            List<clsModels.Slide4Response> slides = new List<clsModels.Slide4Response>();
            clsModels.Slide4Response slide = null;
            string Query = @"select Slide4ID, Title, Description, ImageUrl from Slide4;";
            SqlCommand Command = new SqlCommand(Query, Connection);
            try
            {
                Connection.Open();
                SqlDataReader reader = Command.ExecuteReader();
                while (reader.Read())
                {
                    slide = new clsModels.Slide4Response();
                    slide.Slide4ID = Convert.ToInt32(reader["Slide4ID"]);
                    slide.Title = reader["Title"].ToString();
                    slide.Description = reader["Description"].ToString();
                    slide.ImageUrl = reader["ImageUrl"].ToString();
                    slides.Add(slide);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error is : " + ex.Message);
            }
            finally
            {
                Connection.Close();
            }
            return slides;
        }

        static public bool DeleteSlide4(int slide4ID)
        {
            SqlConnection Connection = new SqlConnection(ConnectionString);
            bool isDeleted = false;
            string Query = @"delete from Slide4 where Slide4ID = @SlideID;";
            SqlCommand Command = new SqlCommand(Query, Connection);
            Command.Parameters.AddWithValue("@SlideID", slide4ID);
            try
            {
                Connection.Open();
                int rowsAffected = Command.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    isDeleted = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error is : " + ex.Message);
            }
            finally
            {
                Connection.Close();
            }
            return isDeleted;
        }

        // Slide 5
        static public bool AddSlide5(clsModels.Slide5 slide, string ImageUrl)
        {
            SqlConnection Connection = new SqlConnection(ConnectionString);
            bool isAdded = false;
            string Query = @"insert into Slide5 (Title, Description, ImageUrl) values (@Title, @Description, @ImageUrl);";
            SqlCommand Command = new SqlCommand(Query, Connection);
            Command.Parameters.AddWithValue("@Title", slide.Title);
            Command.Parameters.AddWithValue("@Description", slide.Description);
            Command.Parameters.AddWithValue("@ImageUrl", ImageUrl);
            try
            {
                Connection.Open();
                int rowsAffected = Command.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    isAdded = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error is : " + ex.Message);
            }
            finally
            {
                Connection.Close();
            }
            return isAdded;
        }

        static public List<clsModels.Slide5Response> GetAllSlides5()
        {
            SqlConnection Connection = new SqlConnection(ConnectionString);
            List<clsModels.Slide5Response> slides = new List<clsModels.Slide5Response>();
            clsModels.Slide5Response slide = null;
            string Query = @"select Slide5ID, Title, Description, ImageUrl from Slide5;";
            SqlCommand Command = new SqlCommand(Query, Connection);
            try
            {
                Connection.Open();
                SqlDataReader reader = Command.ExecuteReader();
                while (reader.Read())
                {
                    slide = new clsModels.Slide5Response();
                    slide.Slide5ID = Convert.ToInt32(reader["Slide5ID"]);
                    slide.Title = reader["Title"].ToString();
                    slide.Description = reader["Description"].ToString();
                    slide.ImageUrl = reader["ImageUrl"].ToString();
                    slides.Add(slide);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error is : " + ex.Message);
            }
            finally
            {
                Connection.Close();
            }
            return slides;
        }

        static public bool DeleteSlide5(int slide5ID)
        {
            SqlConnection Connection = new SqlConnection(ConnectionString);
            bool isDeleted = false;
            string Query = @"delete from Slide5 where Slide5ID = @SlideID;";
            SqlCommand Command = new SqlCommand(Query, Connection);
            Command.Parameters.AddWithValue("@SlideID", slide5ID);
            try
            {
                Connection.Open();
                int rowsAffected = Command.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    isDeleted = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error is : " + ex.Message);
            }
            finally
            {
                Connection.Close();
            }
            return isDeleted;
        }

        // Slide 6
        static public bool AddSlide6(clsModels.Slide6 slide, string ImageUrl)
        {
            SqlConnection Connection = new SqlConnection(ConnectionString);
            bool isAdded = false;
            string Query = @"insert into Slide6 (Title, Description, ImageUrl) values (@Title, @Description, @ImageUrl);";
            SqlCommand Command = new SqlCommand(Query, Connection);
            Command.Parameters.AddWithValue("@Title", slide.Title);
            Command.Parameters.AddWithValue("@Description", slide.Description);
            Command.Parameters.AddWithValue("@ImageUrl", ImageUrl);
            try
            {
                Connection.Open();
                int rowsAffected = Command.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    isAdded = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error is : " + ex.Message);
            }
            finally
            {
                Connection.Close();
            }
            return isAdded;
        }

        static public List<clsModels.Slide6Response> GetAllSlides6()
        {
            SqlConnection Connection = new SqlConnection(ConnectionString);
            List<clsModels.Slide6Response> slides = new List<clsModels.Slide6Response>();
            clsModels.Slide6Response slide = null;
            string Query = @"select Slide6ID, Title, Description, ImageUrl from Slide6;";
            SqlCommand Command = new SqlCommand(Query, Connection);
            try
            {
                Connection.Open();
                SqlDataReader reader = Command.ExecuteReader();
                while (reader.Read())
                {
                    slide = new clsModels.Slide6Response();
                    slide.Slide6ID = Convert.ToInt32(reader["Slide6ID"]);
                    slide.Title = reader["Title"].ToString();
                    slide.Description = reader["Description"].ToString();
                    slide.ImageUrl = reader["ImageUrl"].ToString();
                    slides.Add(slide);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error is : " + ex.Message);
            }
            finally
            {
                Connection.Close();
            }
            return slides;
        }

        static public bool DeleteSlide6(int slide6ID)
        {
            SqlConnection Connection = new SqlConnection(ConnectionString);
            bool isDeleted = false;
            string Query = @"delete from Slide6 where Slide6ID = @SlideID;";
            SqlCommand Command = new SqlCommand(Query, Connection);
            Command.Parameters.AddWithValue("@SlideID", slide6ID);
            try
            {
                Connection.Open();
                int rowsAffected = Command.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    isDeleted = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error is : " + ex.Message);
            }
            finally
            {
                Connection.Close();
            }
            return isDeleted;
        }

        static public bool UpdateSlide(clsModels.Slide1 slide , string Image , int SlideID)
        {
            SqlConnection Connection = new SqlConnection(ConnectionString);
            bool isUpdated = false;
            string Query = @"update Slide1 set Title = @Title , Description = @Description , ImageUrl = @ImageUrl 
                            where Slide1 = @SlideID;";
            SqlCommand Command = new SqlCommand(Query, Connection);
            Command.Parameters.AddWithValue("@Title", slide.Title);
            Command.Parameters.AddWithValue("@Description", slide.Description);
            Command.Parameters.AddWithValue("@ImageUrl", Image);
            Command.Parameters.AddWithValue("@SlideID", SlideID);
            try
            {
                Connection.Open();
                int rowsAffected = Command.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    isUpdated = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error is : " + ex.Message);
            }
            finally
            {
                Connection.Close();
            }
            return isUpdated;
        }

        static public bool AddSlide2(clsModels.Slide2 slide2 ,List<string> Images)
        {
            SqlConnection connection = new SqlConnection(ConnectionString);
            bool isAdded = false;
            string query = @"insert into Slide2 (Title , Description ) values (@Title , @Description );
                             Select Cast (Scope_Identity() As int);";
            SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Title", slide2.Title);
            command.Parameters.AddWithValue("@Description", slide2.Description);
            try
            {
                connection.Open();
                int slide2ID = Convert.ToInt32(command.ExecuteScalar());
                foreach (var imageUrl in Images)
                {
                    string insertImageQuery = @"insert into ImageSlide2 (Slide2ID , Image ) values (@Slide2ID , @ImageUrl );";
                    SqlCommand insertImageCommand = new SqlCommand(insertImageQuery, connection);
                    insertImageCommand.Parameters.AddWithValue("@Slide2ID", slide2ID);
                    insertImageCommand.Parameters.AddWithValue("@ImageUrl", imageUrl);
                    insertImageCommand.ExecuteNonQuery();
                }
                isAdded = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error is : " + ex.Message);
            }
            finally
            {
                connection.Close();
            }
            return isAdded;
        }

        static public List<clsModels.Slide2Response> GetAllSlides2()
        {
            SqlConnection connection = new SqlConnection(ConnectionString);
            List<clsModels.Slide2Response> slides2 = new List<clsModels.Slide2Response>();
            string query = @"select S.Slide2ID , S.Title , S.Description , I.Image 
                             from Slide2 S left join ImageSlide2 I ON S.Slide2ID = I.Slide2ID;";
            SqlCommand command = new SqlCommand(query, connection);
            try
            {
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                Dictionary<int, clsModels.Slide2Response> slideDict = new Dictionary<int, clsModels.Slide2Response>();
                while (reader.Read())
                {
                    int slide2ID = Convert.ToInt32(reader["Slide2ID"]);
                    if (!slideDict.ContainsKey(slide2ID))
                    {
                        slideDict[slide2ID] = new clsModels.Slide2Response
                        {
                            Slide2ID = slide2ID,
                            Title = reader["Title"].ToString(),
                            Description = reader["Description"].ToString(),
                            Images = new List<string>()
                        };
                    }
                    if (reader["Image"] != DBNull.Value)
                    {
                        slideDict[slide2ID].Images.Add(reader["Image"].ToString());
                    }
                }
                reader.Close();
                slides2.AddRange(slideDict.Values);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error is : " + ex.Message);
            }
            finally
            {
                connection.Close();
            }
            return slides2;
        }

        static public bool DeleteSlide2(int Slide2ID)
        {
            SqlConnection Connection = new SqlConnection(ConnectionString);

            // كويري حذف الصور أولاً
            string QueryImages = @"DELETE FROM ImageSlide2 WHERE Slide2ID = @Slide2ID";

            // كويري حذف السلايد نفسه
            string QuerySlide = @"DELETE FROM Slide2 WHERE Slide2ID = @Slide2ID";

            try
            {
                Connection.Open();

                // تنفيذ حذف الصور
                using (SqlCommand cmdImages = new SqlCommand(QueryImages, Connection))
                {
                    cmdImages.Parameters.AddWithValue("@Slide2ID", Slide2ID);
                    cmdImages.ExecuteNonQuery();
                }

                // تنفيذ حذف السلايد
                using (SqlCommand cmdSlide = new SqlCommand(QuerySlide, Connection))
                {
                    cmdSlide.Parameters.AddWithValue("@Slide2ID", Slide2ID);
                    int AffectedRow = cmdSlide.ExecuteNonQuery();

                    if (AffectedRow > 0)
                    {
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.ToString());
            }
            finally
            {
                Connection.Close();
            }

            return false;
        }

        static public bool CreatePaymentSession(clsModels.PaymentSassion session)
        {
            SqlConnection Connection  = new SqlConnection(ConnectionString);
            bool isCreated = false;
            string Query = @"insert into PaymentSession (TransactionReference , PersonID , Amount , PaymentMethod , Status , CreatedAt , ExpiresAt ) 
                            values (@TransactionReference , @PersonID , @Amount , @PaymentMethod , @Status , @CreatedAt , @ExpiresAt );";
            SqlCommand Command = new SqlCommand(Query, Connection); 
            Command.Parameters.AddWithValue("@TransactionReference", session.TransactionReference);
            Command.Parameters.AddWithValue("@PersonID", session.PersonID);
            Command.Parameters.AddWithValue("@Amount", session.Amount);
            Command.Parameters.AddWithValue("@PaymentMethod", session.PaymentMethod);
            Command.Parameters.AddWithValue("@Status", session.Status);
            Command.Parameters.AddWithValue("@CreatedAt", session.CreatedAt);   
            if (session.ExpiresAt.HasValue)
            {
                Command.Parameters.AddWithValue("@ExpiresAt", session.ExpiresAt.Value);
            }
            else
            {
                Command.Parameters.AddWithValue("@ExpiresAt", DBNull.Value);
            }
            try
            {
                Connection.Open();
                int rowsAffected = Command.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    isCreated = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error is : " + ex.Message);
            }
            finally
            {
                Connection.Close();
            }
            return isCreated;
        }

        public static bool UpdatePaymentSessionStatus(string transactionReference, string status)
        {
            SqlConnection Connection = new SqlConnection(ConnectionString);
            bool isUpdated = false;
            string Query = @"UPDATE PaymentSession SET Status = @Status WHERE TransactionReference = @TransactionReference;";
            SqlCommand Command = new SqlCommand(Query, Connection);
            Command.Parameters.AddWithValue("@Status", status);
            Command.Parameters.AddWithValue("@TransactionReference", transactionReference);
            try
            {
                Connection.Open();
                int rowsAffected = Command.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    isUpdated = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error is : " + ex.Message);
            }
            finally
            {
                Connection.Close();
            }
            return isUpdated;
        }

        public static PaymentSassion GetPaymentSessionByPersonID(int personID)
        {
            SqlConnection connection = new SqlConnection(ConnectionString);
            PaymentSassion session = null;

            string query = @"
                            SELECT TOP 1 TransactionReference, PersonID, Amount, PaymentMethod, Status, CreatedAt, ExpiresAt
                            FROM PaymentSession
                            WHERE PersonID = @PersonID
                            ORDER BY CreatedAt DESC;"; 

            SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@PersonID", personID);

            try
            {
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();

                if (reader.Read())
                {
                    session = new PaymentSassion
                    {
                        TransactionReference = reader["TransactionReference"].ToString(),
                        PersonID = Convert.ToInt32(reader["PersonID"]),
                        Amount = Convert.ToDecimal(reader["Amount"]),
                        PaymentMethod = reader["PaymentMethod"].ToString(),
                        Status = reader["Status"].ToString(),
                        CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                        ExpiresAt = reader["ExpiresAt"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["ExpiresAt"]) : null
                    };
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            finally
            {
                connection.Close();
            }

            return session;
        }

        public static DonationPaymentResponse ConfirmDonation(DonationPaymentRequest request)
        {
            DonationPaymentResponse response = new DonationPaymentResponse();
            SqlConnection Connection = new SqlConnection(ConnectionString);
            SqlTransaction transaction = null;

            try
            {
                Connection.Open();
                transaction = Connection.BeginTransaction();

                // ✅ هنجمع كل التبرعات من السلة
                string selectQuery = @"SELECT D.SectionID, D.Amount , S.Name  , S.ImageUrl FROM DonationCart D 
                                        join Section S ON D.SectionID = S.SectionID WHERE PersonID = @PersonID;";
                SqlCommand selectCommand = new SqlCommand(selectQuery, Connection, transaction);
                selectCommand.Parameters.AddWithValue("@PersonID", request.PersonID);
                SqlDataReader reader = selectCommand.ExecuteReader();

                List<(int SectionID, decimal Amount , string Name , string ImageUrl)> donationList = new List<(int, decimal , string Name, string ImageUrl)>();
                while (reader.Read())
                {
                    donationList.Add((
                        Convert.ToInt32(reader["SectionID"]),
                        Convert.ToDecimal(reader["Amount"]),
                        Convert.ToString(reader["Name"]),
                        Convert.ToString(reader["ImageUrl"])
                    ));
                }
                reader.Close();

                foreach (var item in donationList)
                {
                    // ✅ تحديث بيانات القسم
                    string updateQuery = @"UPDATE Section 
                                       SET CollectedAmount = CollectedAmount + @Amount, 
                                           DonorsCount = DonorsCount + 1 
                                       WHERE SectionID = @SectionID;";
                    SqlCommand updateCommand = new SqlCommand(updateQuery, Connection, transaction);
                    updateCommand.Parameters.AddWithValue("@Amount", item.Amount);
                    updateCommand.Parameters.AddWithValue("@SectionID", item.SectionID);
                    updateCommand.ExecuteNonQuery();

                    // ✅ إدخال عملية التبرع
                    string insertQuery = @"INSERT INTO Donation(PersonID, SectionID, Amount, PaymentStatus,DonationDate , SectionName , SectionImage)
                                       VALUES(@PersonID, @SectionID, @Amount,  @PaymentStatus,@DonationDate , @Name , @ImageUrl);";

                    SqlCommand insertCommand = new SqlCommand(insertQuery, Connection, transaction);
                    insertCommand.Parameters.AddWithValue("@PersonID", request.PersonID);
                    insertCommand.Parameters.AddWithValue("@SectionID", item.SectionID);
                    insertCommand.Parameters.AddWithValue("@Amount", item.Amount);
                    insertCommand.Parameters.AddWithValue("@PaymentStatus", "Succeeded");
                    insertCommand.Parameters.AddWithValue("@Name", item.Name);
                    insertCommand.Parameters.AddWithValue("@ImageUrl", item.ImageUrl);
                    insertCommand.Parameters.AddWithValue("@DonationDate", DateTime.Now);
                    insertCommand.ExecuteNonQuery();
                }

                // ✅ حذف السلة بعد التبرع
                string deleteQuery = "DELETE FROM DonationCart WHERE PersonID = @PersonID;";
                SqlCommand deleteCommand = new SqlCommand(deleteQuery, Connection, transaction);
                deleteCommand.Parameters.AddWithValue("@PersonID", request.PersonID);
                deleteCommand.ExecuteNonQuery();
                transaction.Commit();
                response.Success = true;
                response.Message = "Donation confirmed successfully.";
            }
            catch (Exception ex)
            {
                transaction?.Rollback();
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }
            finally
            {
                Connection.Close();
            }

            return response;
        }

    }
}

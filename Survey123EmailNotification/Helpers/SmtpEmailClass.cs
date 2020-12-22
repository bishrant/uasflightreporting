using System;
using System.IO;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Survey123EmailNotification.Helpers
{
    public class SmtpEmailClass
    {
        string smtpClient = AppSettings.Configuration.GetSection("smtpClient").Value;

        public Boolean SendEmailTest(string sendersEmail, string recepient, string msgSubject, string msgText)
        {
            try
            {
                var sClient = new SmtpClient(smtpClient);
                var message = new MailMessage(new MailAddress(sendersEmail), new MailAddress(recepient));
                sClient.UseDefaultCredentials = true;
                sClient.Port = 25;
                sClient.EnableSsl = false;
                message.Body = msgText;
                message.IsBodyHtml = true;
                message.Subject = msgSubject;
                sClient.Send(message);
                return true;
            }
            catch (Exception e)
            {
                string directory = Directory.GetCurrentDirectory();
                using (StreamWriter w = File.AppendText(directory+ "\\wwwroot\\errors\\emailerror.txt"))
                {
                    w.WriteLine(Convert.ToString(e));
                }
                Console.WriteLine("{0} Exception caught.", e);
                return false;
            }
        }

        public async Task<Boolean> SendEmail(string sendersEmail, string recepient, string msgSubject, string msgText)
        {
            var success = false;
            try
            {
                var sClient = new SmtpClient(smtpClient, 25);
                var message = new MailMessage(new MailAddress(sendersEmail), new MailAddress(recepient)) {
                    Subject = msgSubject,
                    IsBodyHtml = true,
                    Body = msgText
                };
                await sClient.SendMailAsync(message);
                sClient.SendCompleted += (s, e) =>
                {
                    sClient.Dispose();
                };
                success = true;
            }
            catch (Exception e)
            {
                string directory = System.IO.Directory.GetCurrentDirectory();
                using (StreamWriter w = File.AppendText(directory + "\\wwwroot\\errors\\emailerror.txt")){w.WriteLine(Convert.ToString(e));}
                Console.WriteLine("{0} Exception caught.", e);
                success = false;
            }
            return success;
        }

        public async Task<Boolean> SendEmailWithDecision(string sendersEmail, string recepient, string msgSubject, string msgText)
        {
            var success = false;
            //try
            //{
                var sClient = new SmtpClient(smtpClient);
                var message = new MailMessage();
                message.To.Add(new MailAddress(sendersEmail));
                message.From = new MailAddress(sendersEmail);
            message.To.Add(new MailAddress(recepient));
            message.Body = msgText;
                message.IsBodyHtml = true;
                message.Subject = msgSubject;

                await sClient.SendMailAsync(message);
                success = true;

            //}
            //catch (Exception e)
            //{
            //    string directory = Directory.GetCurrentDirectory();
            //    using (StreamWriter w = File.AppendText(directory + "\\wwwroot\\errors\\emailerror.txt")) { w.WriteLine(Convert.ToString(e)); }
            //    Console.WriteLine("{0} Exception caught.", e);
            //    success = false;
            //}
            return success;
        }
    }
}

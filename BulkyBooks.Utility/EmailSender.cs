using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBooks.Utility
{
    public class EmailSender : IEmailSender
    {
        //getting values from appsettings.json
        private readonly EmailSendSettings _emailSendSettingser;
        public EmailSender(IOptions<EmailSendSettings> emailSendSettingsAccessor)
        {
            _emailSendSettingser=emailSendSettingsAccessor.Value;
        }

        //DON FORGET TO TURN ON at WORK EMAIL "Less Secure app ACCESS "
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {

            // create email message
            var emailToSend = new MimeMessage();
            emailToSend.From.Add(MailboxAddress.Parse(_emailSendSettingser.EmailUserName.ToString()));//which MAIL WILL SEE CUSTOMER, as who send him a message
            emailToSend.To.Add(MailboxAddress.Parse(email));//where send message(we will get it when we creating user)
            emailToSend.Subject = subject;
            emailToSend .Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = htmlMessage };

            //test OF MY REFERONCE TO appsettings.json
            //string s1 = _emailSendSettingser.EmailUserName.ToString();
            //string s2 = _emailSendSettingser.EmailSmtpAppPassword.ToString();

            //sending message using SMTPclient
            using (var emailClient = new SmtpClient())
            {
                //we are using google SMPTclient CHANGE    SMPTclient CONFIG IF YOU USE OTHER ONE like yandex
                //Какой SMTP yandex?
                //адрес почтового сервера — smtp.yandex.ru; защита соединения — SSL; порт — 465.
                
                emailClient.Connect("smtp.yandex.ru", 465);

                //ailClient.Connect("imap.yandex.ru", 993, MailKit.Security.SecureSocketOptions.Auto);
                //ENTER USERNAME and PASSWORD of ACTUAL(working) EMAIL THAT WILL TAKE(HANDLE) ALL INPUT MESSAGES
                emailClient.Authenticate(_emailSendSettingser.EmailUserName.ToString(), _emailSendSettingser.EmailSmtpAppPassword.ToString());
                await emailClient.SendAsync(emailToSend);
                emailClient.Disconnect(true);
            }

        }
    }
}

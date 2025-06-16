using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Controllers
{
    [ApiController]
    [Route("contact")]
    public class ContactsController : ControllerBase
    {
        public class ContactForm
        {
            public string Name { get; set; } = "";
            public string Email { get; set; } = "";
            public string Message { get; set; } = "";
        }

        [HttpPost]
        public async Task<IActionResult> SendEmail([FromBody] ContactForm form)
        {
            if (string.IsNullOrWhiteSpace(form.Name) || string.IsNullOrWhiteSpace(form.Email) || string.IsNullOrWhiteSpace(form.Message))
            {
                return BadRequest("Missing fields.");
            }

            try
            {
                var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential("maemolol2@gmail.com", "arfhaddhtswnbkke"),
                    EnableSsl = true,
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress("maemolol2@gmail.com"),
                    Subject = $"📬 Message from {form.Name} ({form.Email})",
                    Body = form.Message,
                    IsBodyHtml = false,
                };

                mailMessage.To.Add("maemolol2@gmail.com");

                await smtpClient.SendMailAsync(mailMessage);

                return Ok(new { message = "Email sent successfully!" });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, $"❌ Failed to send email: {ex.Message}");
            }
        }
    }
}

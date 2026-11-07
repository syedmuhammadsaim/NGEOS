using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Options;

namespace NGEOS.Services
{
    public interface IEmailSender
    {
        Task SendPasswordResetCodeAsync(string toEmail, string recipientName, string code, int expirationMinutes = 10);
        Task SendDonationReceiptAsync(string toEmail, string donorName, string transactionId, decimal amount, string donationType, string causeTitle, string ngoName, DateTime donationDate, string cardType, string maskedCardNumber);
        Task SendFriendInvitationAsync(string toEmail, string senderName, string senderEmail, string personalMessage, string? causeTitle = null, string? causeCategory = null, string? causeDescription = null, string? causeImageUrl = null, string? exploreUrl = null);
        Task SendEmailAsync(string toEmail, string subject, string htmlBody, string? plainTextBody = null, string? replyToEmail = null, string? replyToName = null);
    }

    public class EmailSender : IEmailSender
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(IOptions<EmailSettings> options, ILogger<EmailSender> logger)
        {
            _settings = options.Value;
            _logger = logger;
        }

        public async Task SendPasswordResetCodeAsync(string toEmail, string recipientName, string code, int expirationMinutes = 10)
        {
            var subject = $"[{code}] NGEOS Password Reset Verification Code";
            var displayName = string.IsNullOrWhiteSpace(recipientName) ? "NGEOS Member" : recipientName;

            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Password Reset Code</title>
</head>
<body style='margin:0; padding:0; background-color:#f4f6f9; font-family:""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif;'>
    <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0' style='background-color:#f4f6f9; padding:40px 15px;'>
        <tr>
            <td align='center'>
                <table role='presentation' width='100%' style='max-width:580px; background-color:#ffffff; border-radius:12px; overflow:hidden; box-shadow:0 8px 30px rgba(0,0,0,0.08);' cellspacing='0' cellpadding='0' border='0'>
                    <!-- Header -->
                    <tr>
                        <td style='background:linear-gradient(135deg, #f25c05 0%, #ff7a29 100%); padding:32px 30px; text-align:center;'>
                            <h1 style='margin:0; color:#ffffff; font-size:26px; font-weight:700; letter-spacing:0.5px;'>NGE<span style=\'color:#ffe171;\'>OS</span></h1>
                            <p style='margin:6px 0 0; color:#fff0e6; font-size:14px;'>Professional NGO & Charity Platform</p>
                        </td>
                    </tr>
                    <!-- Body Content -->
                    <tr>
                        <td style='padding:36px 32px;'>
                            <h2 style='margin:0 0 16px; color:#1e293b; font-size:20px; font-weight:600;'>Password Reset Verification</h2>
                            <p style='margin:0 0 16px; color:#475569; font-size:15px; line-height:1.6;'>
                                Hello <strong>{System.Net.WebUtility.HtmlEncode(displayName)}</strong>,
                            </p>
                            <p style='margin:0 0 24px; color:#475569; font-size:15px; line-height:1.6;'>
                                We received a request to reset the password for your NGEOS account. Use the 6-digit verification code below to complete the reset process:
                            </p>

                            <!-- Code Box -->
                            <div style='background-color:#fff7ed; border:2px dashed #f25c05; border-radius:10px; padding:22px; text-align:center; margin:0 0 24px;'>
                                <span style='display:inline-block; font-size:36px; font-weight:800; letter-spacing:8px; color:#f25c05; font-family:monospace;'>{code}</span>
                                <p style='margin:10px 0 0; color:#9a3412; font-size:13px; font-weight:500;'>
                                    &#9201; Code expires in <strong>{expirationMinutes} minutes</strong>
                                </p>
                            </div>

                            <p style='margin:0 0 16px; color:#475569; font-size:14px; line-height:1.6;'>
                                Enter this code on the verification screen to proceed with creating your new password. For security reasons, never share this code with anyone.
                            </p>

                            <!-- Security Warning Alert -->
                            <div style='background-color:#fef2f2; border-left:4px solid #ef4444; padding:14px 18px; border-radius:4px; margin-top:20px;'>
                                <p style='margin:0; color:#991b1b; font-size:13px; line-height:1.5;'>
                                    <strong>Security Notice:</strong> If you did not request a password reset, please ignore this email or log in to secure your account. Your password will remain unchanged.
                                </p>
                            </div>
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td style='background-color:#f8fafc; padding:20px 32px; border-top:1px solid #e2e8f0; text-align:center;'>
                            <p style='margin:0; color:#94a3b8; font-size:12px;'>
                                &copy; {DateTime.UtcNow.Year} NGEOS NGO Platform. All rights reserved.
                            </p>
                            <p style='margin:6px 0 0; color:#94a3b8; font-size:12px;'>
                                Need help? Contact us at {System.Net.WebUtility.HtmlEncode(_settings.From)}
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

            var plainTextBody = $@"NGEOS Password Reset Verification
--------------------------------------------------
Hello {displayName},

We received a request to reset the password for your NGEOS account.
Your 6-digit verification code is: {code}

This code expires in {expirationMinutes} minutes.

If you did not request a password reset, please ignore this email.
Support: {_settings.From}";

            await SendEmailAsync(toEmail, subject, htmlBody, plainTextBody);
        }

        public async Task SendDonationReceiptAsync(string toEmail, string donorName, string transactionId, decimal amount, string donationType, string causeTitle, string ngoName, DateTime donationDate, string cardType, string maskedCardNumber)
        {
            var isZakat = donationType.Equals("Zakat", StringComparison.OrdinalIgnoreCase);
            var typeLabel = isZakat ? "Zakat Contribution" : "General Donation";
            var subject = isZakat
                ? $"NGEOS Official Receipt: Zakat Contribution #{transactionId}"
                : $"NGEOS Official Receipt: Donation for {causeTitle} #{transactionId}";
            var displayName = string.IsNullOrWhiteSpace(donorName) ? "Valued Donor" : donorName;

            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>NGEOS Official Donation Receipt</title>
</head>
<body style='margin:0; padding:0; background-color:#f5f0e8; font-family:""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; color:#1c1917;'>
    <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0' style='background-color:#f5f0e8; padding:35px 15px;'>
        <tr>
            <td align='center'>
                <table role='presentation' width='100%' style='max-width:600px; background-color:#ffffff; border-radius:16px; overflow:hidden; box-shadow:0 10px 35px rgba(0,0,0,0.08); border:1px solid #e5ded2;' cellspacing='0' cellpadding='0' border='0'>
                    <!-- Header -->
                    <tr>
                        <td style='background:linear-gradient(135deg, #0b192c 0%, #1e293b 100%); padding:32px 30px; text-align:center;'>
                            <h1 style='margin:0; color:#ffffff; font-size:26px; font-weight:800; letter-spacing:0.5px;'>NGE<span style=\'color:#f25c05;\'>OS</span></h1>
                            <p style='margin:6px 0 0; color:#cbd5e1; font-size:13px; text-transform:uppercase; letter-spacing:1px;'>Official Electronic Donation Receipt</p>
                        </td>
                    </tr>
                    <!-- Body Content -->
                    <tr>
                        <td style='padding:36px 32px;'>
                            <div style='text-align:center; margin-bottom:24px;'>
                                <span style='display:inline-block; background-color:#dcfce7; color:#15803d; font-size:12px; font-weight:700; text-transform:uppercase; letter-spacing:0.8px; padding:6px 16px; border-radius:50px; border:1px solid #bbf7d0;'>
                                    &#10003; Payment Verified & Recorded
                                </span>
                                <h2 style='margin:14px 0 6px; color:#0f172a; font-size:22px; font-weight:700;'>Thank You for Your Generosity!</h2>
                                <p style='margin:0; color:#64748b; font-size:14px;'>Dear <strong>{System.Net.WebUtility.HtmlEncode(displayName)}</strong>, your humanitarian gift directly transforms vulnerable lives.</p>
                            </div>

                            <!-- Amount Box -->
                            <div style='background-color:#fff7ed; border:2px dashed #f25c05; border-radius:12px; padding:20px; text-align:center; margin-bottom:28px;'>
                                <span style='display:block; color:#9a3412; font-size:12px; text-transform:uppercase; font-weight:600; letter-spacing:1px;'>Amount Contributed</span>
                                <span style='display:block; font-size:34px; font-weight:800; color:#15803d; margin:6px 0;'>Rs {amount:N2} <span style='font-size:16px; color:#64748b;'>PKR</span></span>
                                <span style='display:inline-block; background-color:#ffedd5; color:#c2410c; font-size:12px; font-weight:700; padding:4px 12px; border-radius:50px;'>{typeLabel}</span>
                            </div>

                            <!-- Transaction Details Table -->
                            <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0' style='margin-bottom:24px; border-collapse:collapse;'>
                                <tr style='border-bottom:1px solid #f1f5f9;'>
                                    <td style='padding:10px 0; color:#64748b; font-size:13px;'>Receipt / Txn ID:</td>
                                    <td style='padding:10px 0; text-align:right; font-weight:700; color:#0f172a; font-size:13px; font-family:monospace;'>{transactionId}</td>
                                </tr>
                                <tr style='border-bottom:1px solid #f1f5f9;'>
                                    <td style='padding:10px 0; color:#64748b; font-size:13px;'>Contribution Purpose:</td>
                                    <td style='padding:10px 0; text-align:right; font-weight:600; color:#0f172a; font-size:13px;'>{System.Net.WebUtility.HtmlEncode(causeTitle)}</td>
                                </tr>
                                <tr style='border-bottom:1px solid #f1f5f9;'>
                                    <td style='padding:10px 0; color:#64748b; font-size:13px;'>Managing Partner NGO:</td>
                                    <td style='padding:10px 0; text-align:right; color:#0f172a; font-size:13px;'>{System.Net.WebUtility.HtmlEncode(ngoName)}</td>
                                </tr>
                                <tr style='border-bottom:1px solid #f1f5f9;'>
                                    <td style='padding:10px 0; color:#64748b; font-size:13px;'>Transaction Date:</td>
                                    <td style='padding:10px 0; text-align:right; color:#0f172a; font-size:13px;'>{donationDate:MMM dd, yyyy - hh:mm tt} UTC</td>
                                </tr>
                                <tr>
                                    <td style='padding:10px 0; color:#64748b; font-size:13px;'>Payment Mode:</td>
                                    <td style='padding:10px 0; text-align:right; color:#0f172a; font-size:13px;'>{cardType} ({maskedCardNumber})</td>
                                </tr>
                            </table>

                            {(isZakat ? @"
                            <!-- Zakat Transparency Box -->
                            <div style='background-color:#f0fdf4; border-left:4px solid #16a34a; padding:14px 18px; border-radius:6px; margin-bottom:20px;'>
                                <p style='margin:0; color:#14532d; font-size:13px; line-height:1.5;'>
                                    <strong>&#10003; 100% Zakat Policy:</strong> This contribution has been designated as Zakat and is deposited into NGEOS's audited Zakat ledger, distributed exclusively to Shariah-eligible beneficiaries (Mustahiqeen).
                                </p>
                            </div>" : "")}

                            <!-- Official Tax Note -->
                            <div style='background-color:#f8fafc; border:1px solid #e2e8f0; padding:14px 18px; border-radius:6px; margin-bottom:10px;'>
                                <p style='margin:0; color:#475569; font-size:12px; line-height:1.5;'>
                                    <strong>Official Non-Profit Certification:</strong> NGEOS is a certified transparent non-governmental organization. This electronic receipt serves as official proof of payment for your personal tax and donation accounting records.
                                </p>
                            </div>
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td style='background-color:#f8fafc; padding:22px 32px; border-top:1px solid #e2e8f0; text-align:center;'>
                            <p style='margin:0; color:#64748b; font-size:12px; font-weight:600;'>NGEOS Central Secretariat &bull; Karachi, Pakistan</p>
                            <p style='margin:6px 0 0; color:#94a3b8; font-size:11px;'>For assistance or inquiries regarding this receipt, email {System.Net.WebUtility.HtmlEncode(_settings.From)}</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

            var plainTextBody = $@"NGEOS Official Donation Receipt
--------------------------------------------------
Dear {displayName},

Thank you for your generous contribution to NGEOS.

Receipt ID: {transactionId}
Amount: Rs {amount:N2} PKR
Type: {typeLabel}
Purpose: {causeTitle}
Partner NGO: {ngoName}
Date: {donationDate:MMM dd, yyyy - hh:mm tt} UTC
Payment: {cardType} ({maskedCardNumber})
{(isZakat ? "\n100% Zakat Policy: Distributed exclusively to eligible beneficiaries (Mustahiqeen)." : "")}

Official Non-Profit Certification: This electronic receipt serves as official proof of payment for your personal tax records.
NGEOS Central Secretariat, Karachi, Pakistan
Inquiries: {_settings.From}";

            await SendEmailAsync(toEmail, subject, htmlBody, plainTextBody);
        }

        public async Task SendFriendInvitationAsync(
            string toEmail,
            string senderName,
            string senderEmail,
            string personalMessage,
            string? causeTitle = null,
            string? causeCategory = null,
            string? causeDescription = null,
            string? causeImageUrl = null,
            string? exploreUrl = null)
        {
            var hasCause = !string.IsNullOrWhiteSpace(causeTitle);
            var subject = hasCause
                ? $"{senderName} invited you to support \"{causeTitle}\" on NGEOS"
                : $"{senderName} invited you to join NGEOS Charity Platform";

            var ctaUrl = !string.IsNullOrWhiteSpace(exploreUrl) ? exploreUrl : "https://localhost:5125";
            var encodedSenderName = System.Net.WebUtility.HtmlEncode(senderName);
            var encodedSenderEmail = System.Net.WebUtility.HtmlEncode(senderEmail);
            var encodedMessage = System.Net.WebUtility.HtmlEncode(personalMessage);

            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>NGEOS Invitation</title>
</head>
<body style='margin:0; padding:0; background-color:#f4f6f9; font-family:""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; color:#1e293b;'>
    <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0' style='background-color:#f4f6f9; padding:40px 15px;'>
        <tr>
            <td align='center'>
                <table role='presentation' width='100%' style='max-width:600px; background-color:#ffffff; border-radius:14px; overflow:hidden; box-shadow:0 8px 30px rgba(0,0,0,0.08); border:1px solid #e2e8f0;' cellspacing='0' cellpadding='0' border='0'>
                    <!-- Header -->
                    <tr>
                        <td style='background:linear-gradient(135deg, #0b192c 0%, #1e293b 100%); padding:32px 30px; text-align:center;'>
                            <h1 style='margin:0; color:#ffffff; font-size:26px; font-weight:800; letter-spacing:0.5px;'>NGE<span style=\'color:#f25c05;\'>OS</span></h1>
                            <p style='margin:6px 0 0; color:#cbd5e1; font-size:13px; text-transform:uppercase; letter-spacing:1px;'>Verified NGO &amp; Charity Platform</p>
                        </td>
                    </tr>
                    <!-- Body Content -->
                    <tr>
                        <td style='padding:36px 32px;'>
                            <div style='text-align:center; margin-bottom:20px;'>
                                <span style='display:inline-block; background-color:#ffedd5; color:#c2410c; font-size:12px; font-weight:700; text-transform:uppercase; letter-spacing:0.8px; padding:6px 16px; border-radius:50px; border:1px solid #fed7aa;'>
                                    Personal Invitation
                                </span>
                            </div>

                            <h2 style='margin:0 0 14px; color:#0f172a; font-size:22px; font-weight:700; text-align:center;'>
                                You're Invited to Make a Difference!
                            </h2>

                            <p style='margin:0 0 20px; color:#475569; font-size:15px; line-height:1.6;'>
                                Hello! Your friend <strong>{encodedSenderName}</strong> (<a href='mailto:{encodedSenderEmail}' style='color:#f25c05; text-decoration:none;'>{encodedSenderEmail}</a>) has invited you to support humanitarian initiatives on <strong>NGEOS</strong>.
                            </p>

                            <!-- Personal Note Box -->
                            <div style='background-color:#fff7ed; border-left:4px solid #f25c05; border-radius:8px; padding:18px 20px; margin:0 0 26px;'>
                                <span style='display:block; font-size:12px; font-weight:700; text-transform:uppercase; letter-spacing:0.8px; color:#c2410c; margin-bottom:8px;'>
                                    Personal Note from {encodedSenderName}:
                                </span>
                                <p style='margin:0; color:#9a3412; font-size:15px; font-style:italic; line-height:1.6;'>
                                    &ldquo;{encodedMessage}&rdquo;
                                </p>
                            </div>

                            {(hasCause ? $@"
                            <!-- Featured Cause Card -->
                            <div style='background-color:#f8fafc; border:1px solid #e2e8f0; border-radius:12px; overflow:hidden; margin:0 0 26px;'>
                                {(!string.IsNullOrWhiteSpace(causeImageUrl) ? $@"<img src='{causeImageUrl}' alt='{System.Net.WebUtility.HtmlEncode(causeTitle)}' style='width:100%; max-height:220px; object-fit:cover; display:block;' />" : "")}
                                <div style='padding:22px;'>
                                    {(!string.IsNullOrWhiteSpace(causeCategory) ? $@"<span style='display:inline-block; background-color:#ffedd5; color:#c2410c; font-size:11px; font-weight:700; text-transform:uppercase; letter-spacing:0.8px; padding:4px 10px; border-radius:50px; margin-bottom:10px;'>{System.Net.WebUtility.HtmlEncode(causeCategory)}</span>" : "")}
                                    <h3 style='margin:0 0 10px; color:#0f172a; font-size:18px; font-weight:700;'>{System.Net.WebUtility.HtmlEncode(causeTitle)}</h3>
                                    {(!string.IsNullOrWhiteSpace(causeDescription) ? $@"<p style='margin:0; color:#64748b; font-size:14px; line-height:1.6;'>{System.Net.WebUtility.HtmlEncode(causeDescription)}</p>" : "")}
                                </div>
                            </div>

                            <div style='text-align:center; margin:0 0 28px;'>
                                <a href='{ctaUrl}' style='display:inline-block; background-color:#f25c05; color:#ffffff; font-size:15px; font-weight:700; text-decoration:none; padding:14px 34px; border-radius:8px; box-shadow:0 4px 14px rgba(242, 92, 5, 0.35);'>
                                    View Cause &amp; Support &rarr;
                                </a>
                            </div>" : $@"
                            <!-- Platform Highlights -->
                            <div style='background-color:#f8fafc; border:1px solid #e2e8f0; border-radius:12px; padding:22px; margin:0 0 26px;'>
                                <h3 style='margin:0 0 12px; color:#0f172a; font-size:17px; font-weight:700;'>Why Support Through NGEOS?</h3>
                                <p style='margin:0 0 14px; color:#64748b; font-size:14px; line-height:1.6;'>
                                    NGEOS connects compassionate individuals with verified grassroots NGOs working in healthcare, child welfare, education, disabled support, and disaster relief.
                                </p>
                                <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0'>
                                    <tr>
                                        <td style='padding:6px 0; color:#334155; font-size:13px;'>&#10003; <strong>100% Transparency:</strong> Complete tracking of all donations.</td>
                                    </tr>
                                    <tr>
                                        <td style='padding:6px 0; color:#334155; font-size:13px;'>&#10003; <strong>Verified Non-Profits:</strong> Thoroughly vetted NGO partners.</td>
                                    </tr>
                                    <tr>
                                        <td style='padding:6px 0; color:#334155; font-size:13px;'>&#10003; <strong>Instant Official Receipts:</strong> Verified electronic documentation.</td>
                                    </tr>
                                </table>
                            </div>

                            <div style='text-align:center; margin:0 0 28px;'>
                                <a href='{ctaUrl}' style='display:inline-block; background-color:#f25c05; color:#ffffff; font-size:15px; font-weight:700; text-decoration:none; padding:14px 34px; border-radius:8px; box-shadow:0 4px 14px rgba(242, 92, 5, 0.35);'>
                                    Explore Active Causes &rarr;
                                </a>
                            </div>")}

                            <div style='background-color:#f1f5f9; border-radius:8px; padding:14px 18px; text-align:center;'>
                                <p style='margin:0; color:#475569; font-size:13px;'>
                                    Want to reply to {encodedSenderName}? You can email them directly at <a href='mailto:{encodedSenderEmail}' style='color:#f25c05; font-weight:600;'>{encodedSenderEmail}</a>.
                                </p>
                            </div>
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td style='background-color:#f8fafc; padding:22px 32px; border-top:1px solid #e2e8f0; text-align:center;'>
                            <p style='margin:0; color:#64748b; font-size:12px; font-weight:600;'>NGEOS Central Secretariat &bull; Karachi, Pakistan</p>
                            <p style='margin:6px 0 0; color:#94a3b8; font-size:11px;'>
                                You received this invitation because {encodedSenderName} sent it to {System.Net.WebUtility.HtmlEncode(toEmail)}. If you did not expect this, you can safely ignore this email.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

            var plainText = $@"NGEOS Invitation
--------------------------------------------------
Hello,

Your friend {senderName} ({senderEmail}) has invited you to support humanitarian causes on NGEOS.

Personal Note from {senderName}:
""{personalMessage}""

{(hasCause ? $"Cause: {causeTitle}\n{causeDescription}\n\nVisit: {ctaUrl}" : $"Explore causes: {ctaUrl}")}

NGEOS Central Secretariat, Karachi, Pakistan";

            await SendEmailAsync(toEmail, subject, htmlBody, plainText, senderEmail, senderName);
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody, string? plainTextBody = null, string? replyToEmail = null, string? replyToName = null)
        {
            if (string.IsNullOrWhiteSpace(_settings.Host))
            {
                throw new InvalidOperationException("SMTP Host is not configured. Please configure Smtp settings in appsettings.json, User Secrets, or Environment Variables.");
            }

            if (string.IsNullOrWhiteSpace(_settings.From))
            {
                throw new InvalidOperationException("SMTP 'From' address is not configured. Please configure Smtp:From in appsettings.json or environment variables.");
            }

            var fromAddress = new MailAddress(_settings.From, string.IsNullOrWhiteSpace(_settings.SenderName) ? "NGEOS" : _settings.SenderName);
            var toAddress = new MailAddress(toEmail.Trim());

            using var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                SubjectEncoding = Encoding.UTF8,
                BodyEncoding = Encoding.UTF8,
                Priority = MailPriority.Normal
            };

            // Set Reply-To
            if (!string.IsNullOrWhiteSpace(replyToEmail))
            {
                try
                {
                    message.ReplyToList.Add(new MailAddress(replyToEmail.Trim(), replyToName ?? replyToEmail));
                }
                catch
                {
                    message.ReplyToList.Add(fromAddress);
                }
            }
            else
            {
                message.ReplyToList.Add(fromAddress);
            }

            // RFC standard transactional email headers to pass spam filters
            message.Headers.Add("X-Mailer", "NGEOS-Portal");
            message.Headers.Add("Auto-Submitted", "auto-generated");
            message.Headers.Add("X-Auto-Response-Suppress", "All");

            // Format standard Message-ID
            var domain = _settings.From.Contains('@') ? _settings.From.Split('@').Last() : "gmail.com";
            message.Headers.Add("Message-ID", $"<{Guid.NewGuid():N}@{domain}>");

            // Multi-part Alternative (Plain Text + HTML)
            if (string.IsNullOrWhiteSpace(plainTextBody))
            {
                plainTextBody = System.Text.RegularExpressions.Regex.Replace(htmlBody, "<[^>]*>", " ")
                    .Replace("&nbsp;", " ")
                    .Replace("&amp;", "&")
                    .Trim();
            }

            var plainTextView = AlternateView.CreateAlternateViewFromString(plainTextBody, Encoding.UTF8, "text/plain");
            var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, Encoding.UTF8, "text/html");

            message.AlternateViews.Add(plainTextView);
            message.AlternateViews.Add(htmlView);
            message.IsBodyHtml = true;

                        // Check if SMTP Password is configured
            if (string.IsNullOrWhiteSpace(_settings.Password))
            {
                _logger.LogWarning("[EMAIL SIMULATION] SMTP Password is not configured. Email to {ToEmail} saved locally as preview. To enable live delivery to Gmail inboxes, set a 16-character Google App Password in appsettings.json or .env (SMTP_PASSWORD).", toEmail);

                try
                {
                    var previewDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp_receipts");
                    Directory.CreateDirectory(previewDir);
                    var cleanSubject = string.Concat(subject.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
                    if (cleanSubject.Length > 40) cleanSubject = cleanSubject.Substring(0, 40);
                    var safeFileName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{cleanSubject}.html";
                    var filePath = Path.Combine(previewDir, safeFileName);
                    await File.WriteAllTextAsync(filePath, htmlBody, Encoding.UTF8);
                    _logger.LogInformation("[EMAIL SIMULATION] Local email preview written to {Path}", filePath);
                }
                catch (Exception diskEx)
                {
                    _logger.LogWarning(diskEx, "[EMAIL SIMULATION] Could not write local email preview file.");
                }

                return;
            }

            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 15000 // 15 seconds timeout
            };

            if (!string.IsNullOrWhiteSpace(_settings.Username) && !string.IsNullOrWhiteSpace(_settings.Password))
            {
                client.UseDefaultCredentials = false;
                var cleanPassword = _settings.Password.Replace(" ", "").Trim();
                client.Credentials = new NetworkCredential(_settings.Username.Trim(), cleanPassword);
            }

            try
            {
                _logger.LogInformation("Attempting to send email via SMTP host {Host}:{Port} to {ToEmail}", _settings.Host, _settings.Port, toEmail);
                await client.SendMailAsync(message);
                _logger.LogInformation("Email sent successfully to {ToEmail}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SMTP delivery failed for recipient {ToEmail} via {Host}:{Port}. Saving local preview.", toEmail, _settings.Host, _settings.Port);
                try
                {
                    var previewDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp_receipts");
                    Directory.CreateDirectory(previewDir);
                    var cleanSubject = string.Concat(subject.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
                    if (cleanSubject.Length > 40) cleanSubject = cleanSubject.Substring(0, 40);
                    var safeFileName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_FAILED_{cleanSubject}.html";
                    var filePath = Path.Combine(previewDir, safeFileName);
                    await File.WriteAllTextAsync(filePath, htmlBody, Encoding.UTF8);
                }
                catch { }
                throw;
            }
        }
    }
}

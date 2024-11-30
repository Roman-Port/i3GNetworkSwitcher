using i3GNetworkSwitcher.Core.Config;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace i3GNetworkSwitcher.Core.Notifier
{
    internal class NetworkEmailer : INetworkNotifier
    {
        public NetworkEmailer(ConfEmail config)
        {
            //Set up client
            smtp = new SmtpClient(config.SmtpHost);
            smtp.EnableSsl = config.UseSsl;
            if (config.SmtpUsername != null && config.SmtpPassword != null)
            {
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential(config.SmtpUsername, config.SmtpPassword);
            }
            smtp.SendCompleted += Smtp_SendCompleted;

            //Set up email addresses
            from = new MailAddress(config.FromEmail, config.FromName);
            if (config.Lists == null)
                throw new Exception("No lists were supplied.");
            foreach (var l in config.Lists)
            {
                if (Enum.TryParse(l.Key.ToUpper(), out AlertLevel level))
                {
                    if (l.Value.Length > 0)
                        lists.Add(level, l.Value.Select(x => new MailAddress(x)).ToArray());
                }
                else
                {
                    throw new Exception($"E-Mail alert level \"{l.Key}\" is not valid.");
                }
            }
        }

        private readonly SmtpClient smtp;
        private readonly MailAddress from;
        private readonly Dictionary<AlertLevel, MailAddress[]> lists = new Dictionary<AlertLevel, MailAddress[]>();

        public void SendAlert(AlertLevel level, string subject, string body)
        {
            //Get list
            if (lists.TryGetValue(level, out MailAddress[] addresses))
            {
                //Create a async state and invoke method as if returning from an async call
                UserAsyncState state = new UserAsyncState
                {
                    Subject = subject,
                    Body = body,
                    RemainingEmails = new Queue<MailAddress>(addresses)
                };

                //Invoke as if returning from an async call
                ProcessStage(state);
            }
        }

        private void Smtp_SendCompleted(object sender, AsyncCompletedEventArgs e)
        {
            //Get state
            UserAsyncState state = (UserAsyncState)e.UserState;

            //Check if an error occured
            if (e.Error != null)
                Console.WriteLine($"[EMAIL] Error sending to {state.LastAddress.Address}: {e.Error.Message}");

            //Continue to next
            ProcessStage(state);
        }

        private void ProcessStage(UserAsyncState state)
        {
            //Pop email address
            if (state.RemainingEmails.TryDequeue(out MailAddress to))
            {
                //Create message
                MailMessage msg = new MailMessage(from, to);
                msg.Subject = state.Subject;
                msg.Body = state.Body;

                //Send
                try
                {
                    state.LastAddress = to;
                    smtp.SendAsync(msg, state);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[EMAIL] Failed to start sending async email to {to.Address}. Aborting... Error: {ex.Message}");
                }
            }
        }

        class UserAsyncState
        {
            public string Subject { get; set; }
            public string Body { get; set; }
            public Queue<MailAddress> RemainingEmails { get; set; }
            public MailAddress LastAddress { get; set; }
        }
    }
}

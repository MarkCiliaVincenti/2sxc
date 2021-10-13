﻿using System;
using System.Net;
using System.Net.Mail;
using DotNetNuke.Entities.Host;
using ToSic.Sxc.Services;

namespace ToSic.Sxc.Dnn.Services
{
    public class DnnMailService : MailServiceBase
    {
        protected override SmtpClient SmtpClient()
        {
            var smtpServer = Host.SMTPServer;
            if (string.IsNullOrEmpty(smtpServer)) return null; // DotNetNuke.Services.Localization.Localization.GetString("SMTPConfigurationProblem");

            var client = new SmtpClient();

            var strArray = smtpServer.Split(':');
            client.Host = strArray[0];
            client.Port = strArray.Length > 1 ? Convert.ToInt32(strArray[1]) : 25;
            client.ServicePoint.MaxIdleTime = Host.SMTPMaxIdleTime;
            client.ServicePoint.ConnectionLimit = Host.SMTPConnectionLimit;

            var smtpAuthentication = Host.SMTPAuthentication;
            var smtpUsername = Host.SMTPUsername;
            var smtpPassword = Host.SMTPPassword;

            switch (smtpAuthentication)
            {
                case "1":
                    if (!string.IsNullOrEmpty(smtpUsername) && !string.IsNullOrEmpty(smtpPassword))
                    {
                        client.UseDefaultCredentials = false;
                        client.Credentials = (ICredentialsByHost)new NetworkCredential(smtpUsername, smtpPassword);
                        break;
                    }
                    break;
                case "2":
                    client.UseDefaultCredentials = true;
                    break;
            }

            client.EnableSsl = Host.EnableSMTPSSL;

            return client;
        }
    }
}
﻿using System.Net.Mail;
using System.Text;
using ToSic.Eav.Documentation;
using ToSic.Sxc.Code;

// ReSharper disable once CheckNamespace
namespace ToSic.Sxc.Services
{
    /// <summary>
    /// Service to send mail messages cross-platform.
    ///
    /// Get this service in Razor or WebApi using [](xref:ToSic.Sxc.Code.IDynamicCode.GetService*)
    /// </summary>
    /// <remarks>
    /// New in 2sxc 12.05
    /// </remarks>
    [PublicApi]
    public interface IMailService: INeedsCodeRoot
    {
        /// <summary>
        /// Quickly create a MailMessage object for further modification and then sending using <see cref="Send(MailMessage)"/>
        /// If you don't want to modify the resulting object, skip this and use the direct-send method. 
        /// </summary>
        /// <param name="noParamOrder">see [](xref:NetCode.Conventions.NamedParameters)</param>
        /// <param name="from">
        ///     sender e-mail address in one of the following formats
        ///     * An e-mail string like "info@somwhere.com"
        ///     * An e-mail with name and address like "iJungleboy &lt;ijungleboy@2sxc.org&gt;"
        ///     * A single System.Net.Mail.MailAddress object
        /// </param>
        /// <param name="to">
        ///     Main recipient(s) of the mail in one of the following formats
        ///     * all the formats as available in <see cref="from"/>
        ///     * a CSV of such addresses like "info@a.com, info@b.com"
        ///     * An Array/List/IEnumerable of such strings
        ///     * An Array/List/IEnumerable of System.Net.Mail.MailAddress objects
        /// </param>
        /// <param name="cc">CC recipient(s) of the mail, in the same format as <see cref="to"/></param>
        /// <param name="bcc">BCC recipient(s) of the mail, in the same format as <see cref="to"/></param>
        /// <param name="replyTo">ReplyTo address(es) in the same format as <see cref="to"/></param>
        /// <param name="subject">The main subject</param>
        /// <param name="body">The body / contents of the e-mail - can be text or HTML</param>
        /// <param name="isHtml">Set the body to be HTML - if not set, will auto-detect</param>
        /// <param name="encoding">
        ///     Encoding of subject and body - if not set, will default to UTF8.
        ///     If you need different encodings on subject and body, set it on the resulting object. 
        /// </param>
        /// <param name="attachments">
        ///     One or more attachments to include. Could be any of the following
        ///     - A System.Net.Mail.Attachment object
        ///     - An <see cref="Adam.IFile"/> or an <see cref="Eav.Apps.Assets.IFile"/> object
        ///     - An Array/IEnumerable of these 
        /// </param>
        /// <returns>The newly created `MailMessage` object</returns>
        MailMessage Create(
            string noParamOrder = Eav.Parameters.Protector,
            object from = null,
            object to = null,
            object cc = null,
            object bcc = null,
            object replyTo = null,
            string subject = null,
            string body = null,
            bool? isHtml = null,
            Encoding encoding = null,
            object attachments = null);

        /// <summary>
        /// Send a .net `MailMessage` object using the settings configured in Dnn or Oqtane.
        /// </summary>
        /// <param name="message">A prepared .net MailMessage object</param>
        /// <returns></returns>
        void Send(MailMessage message);

        /// <summary>
        /// Quickly create and send an E-Mail.
        /// </summary>
        /// <param name="noParamOrder">see [](xref:NetCode.Conventions.NamedParameters)</param>
        /// <param name="from">
        ///     sender e-mail address in one of the following formats
        ///     * An e-mail string like "info@somwhere.com"
        ///     * An e-mail with name and address like "iJungleboy &lt;ijungleboy@2sxc.org&gt;"
        ///     * A single System.Net.Mail.MailAddress object
        /// </param>
        /// <param name="to">
        ///     Main recipient(s) of the mail in one of the following formats
        ///     * all the formats as available in <see cref="from"/>
        ///     * a CSV of such addresses like "info@a.com, info@b.com"
        ///     * An Array/List/IEnumerable of such strings
        ///     * An Array/List/IEnumerable of System.Net.Mail.MailAddress objects
        /// </param>
        /// <param name="cc">CC recipient(s) of the mail, in the same format as <see cref="to"/></param>
        /// <param name="bcc">BCC recipient(s) of the mail, in the same format as <see cref="to"/></param>
        /// <param name="replyTo">ReplyTo address(es) in the same format as <see cref="to"/></param>
        /// <param name="subject">The main subject</param>
        /// <param name="body">The body / contents of the e-mail - can be text or HTML</param>
        /// <param name="isHtml">Set the body to be HTML - if not set, will auto-detect</param>
        /// <param name="encoding">
        ///     Encoding of subject and body - if not set, will default to UTF8.
        ///     If you need different encodings on subject and body, set it on the resulting object. 
        /// </param>
        /// <param name="attachments">
        ///     One or more attachments to include. Could be any of the following
        ///     - A System.Net.Mail.Attachment object
        ///     - An <see cref="ToSic.Sxc.Adam.IFile"/> or an <see cref="ToSic.Eav.Apps.Assets.IFile"/> object
        ///     - An Array/IEnumerable of these 
        /// </param>
        /// <returns>void</returns>
        void Send(
            string noParamOrder = Eav.Parameters.Protector,
            object from = null,
            object to = null,
            object cc = null,
            object bcc = null,
            object replyTo = null,
            string subject = null,
            string body = null,
            bool? isHtml = null,
            Encoding encoding = null,
            object attachments = null
        );

        [PrivateApi] MailAddress MailAddress(string addressType, object mailAddress);

        [PrivateApi] bool AddMailAddresses(string addressType, MailAddressCollection targetMails, object mailAddresses);
    }
}
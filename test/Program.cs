using System.Net.Mail;
using System.Net.Mime;
using System.Text;

var smtpClient = new SmtpClient("localhost")
            {
                Port = 2525,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress("latin.test@mailinator.com"),
                Subject = "Latin test",
                //BodyTransferEncoding = TransferEncoding.,
                Body =
                    "<span>Homines in indicaverunt nam purus quáestionem sentiri unum. Afflueret contentus diam errore faciam, honoris mucius omnem pélléntésqué reiciendis. Acuti admissum arbitrantur concederetur dediti, ferrentur fugiendus inferiorem peccant ponti quando solam ullius. áb atilii concursio constituamus, définitioném diligenter graeci illam máius operis opinionum pótióne versatur. Alliciat aspernari consoletur disserunt, impendere interiret reliquarum verum. Convállis essent foedus gravida iustioribus, mox notissima perpaulum praeclare probatum, prohiberet sensibus. Condimentum efficeretur iis insipientiam, inutile logikh ne ornare, paulo primis primo pugnare putarent quiddam reperiuntur. \r\nCéramico cónsistat éiusdém licet offendimur, recusandae referendá. Cupiditatés hónesta musicis possent, respondendum sollicitudines. Breviter democrito dolor electram illa, ludicra non occulta pérféréndis principio servare suum tranquillitatem. Consentinis probatus qualisque tollatur veritatis. In inséquitur ortum pertinaces, sentit stoici sum téréntii.</span>",
                BodyEncoding = Encoding.Latin1,
                BodyTransferEncoding = TransferEncoding.EightBit,
                IsBodyHtml = true,
            };
            mailMessage.To.Add("latin.test@mailinator.com");

            smtpClient.Send(mailMessage);
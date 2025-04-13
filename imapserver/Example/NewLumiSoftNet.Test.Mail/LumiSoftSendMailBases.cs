using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using LumiSoft.Net.Mime;
using LumiSoft.Net.SMTP.Client;
using LumiSoft.Net.SMTP;
using LumiSoft.Net;

namespace NewLumiSoftNet.Test.Mail
{
    public class LumiSoftSendMailBases
    {
        public class LumiSoftSendMailBaseArgs
        {
            public string EmailAccount = "";//发送账号
            public string EmailAddress = "";//发送邮件地址
            public string To = "";//接收人
            public string CC = "";//抄送人
            public string BCC = "";//密件抄送人（暗送人）
            public Mime MimeObject = new Mime();//邮件内容对象
            public MemoryStream MimeStreams = new MemoryStream();//邮件对象流
            public bool IsCancel = false;//是否取消发送    
            /// <summary>
            /// 初始化
            /// </summary>
            /// <param name="strEmailAccout">发送账号</param>
            /// <param name="strEmailAddress">发送邮件地址</param>
            /// <param name="strTo">接收人</param>
            /// <param name="strCC">抄送</param>
            /// <param name="strBCC">暗送人</param>
            /// <param name="myMimeObjects">邮件内容对象</param>
            /// <param name="myMimeStreams">邮件流对象</param>
            public LumiSoftSendMailBaseArgs(string strEmailAccout, string strEmailAddress, string strTo, string strCC, string strBCC, Mime myMimeObjects, MemoryStream myMimeStreams)
            {
                this.EmailAccount = strEmailAccout;
                this.EmailAddress = strEmailAddress;
                this.To = strTo;
                this.CC = strCC;
                this.BCC = strBCC;
                this.MimeObject = myMimeObjects;
                this.MimeStreams = myMimeStreams;
            }
        }
        /// <summary>
        /// 正在发送邮件的委托（尚未发送出去）
        /// </summary>
        /// <param name="e"></param>
        public delegate void SendMailingHandle(LumiSoftSendMailBaseArgs e);
        /// <summary>
        /// 正在发送邮件触发的事件
        /// </summary>
        public event SendMailingHandle SendMailing;
        private LumiSoftSendMailBaseArgs OnSendMailing(string strEmailAccout, string strEmailAddress, string strTo, string strCC, string strBCC, Mime myMimeObjects, MemoryStream myMimeStreams)
        {
            LumiSoftSendMailBaseArgs e = new LumiSoftSendMailBaseArgs(strEmailAccout, strEmailAddress, strTo, strCC, strBCC, myMimeObjects, myMimeStreams);
            if (SendMailing != null)
                SendMailing(e);
            return e;
        }
        public class LumiSoftSendMailSuccessedArgs
        {
            public string EmailAccount = "";//发送账号
            public string EmailAddress = "";//发送邮件地址
            public string To = "";//接收人
            public string CC = "";//抄送人
            public string BCC = "";//密件抄送人（暗送人）
            public Mime MimeObject = new Mime();//邮件内容对象
            public MemoryStream MimeStreams = new MemoryStream();//邮件对象流
            public LumiSoftSendMailSuccessedArgs(string strEmailAccout, string strEmailAddress, string strTo, string strCC, string strBCC, Mime myMimeObjects, MemoryStream myMimeStreams)
            {
                this.EmailAccount = strEmailAccout;
                this.EmailAddress = strEmailAddress;
                this.To = strTo;
                this.CC = strCC;
                this.BCC = strBCC;
                this.MimeObject = myMimeObjects;
                this.MimeStreams = myMimeStreams;
            }
        }
        /// <summary>
        /// 邮件发送成功的委托
        /// </summary>
        /// <param name="e"></param>
        public delegate void SendMailSuccessedHandle(LumiSoftSendMailSuccessedArgs e);
        /// <summary>
        /// 发送邮件成功后触发的事件
        /// </summary>
        public event SendMailSuccessedHandle SendMailSuccessed;
        private void OnSendMailSuccessed(string strEmailAccout, string strEmailAddress, string strTo, string strCC, string strBCC, Mime myMimeObjects, MemoryStream myMimeStreams)
        {
            LumiSoftSendMailSuccessedArgs e = new LumiSoftSendMailSuccessedArgs(strEmailAccout, strEmailAddress, strTo, strCC, strBCC, myMimeObjects, myMimeStreams);
            if (SendMailSuccessed != null)
                SendMailSuccessed(e);
        }
        public class LumiSoftSendMailErrorArgs
        {
            public string EmailAccount = "";//发送账号
            public string EmailAddress = "";//发送邮件地址
            public string To = "";//接收人
            public string CC = "";//抄送人
            public string BCC = "";//密件抄送人（暗送人）
            public Mime MimeObject = new Mime();//邮件内容对象
            public MemoryStream MimeStreams = new MemoryStream();//邮件对象流
            public Exception ex = new Exception();
            public LumiSoftSendMailErrorArgs(string strEmailAccout, string strEmailAddress, string strTo, string strCC, string strBCC, Mime myMimeObjects, MemoryStream myMimeStreams, Exception myEx)
            {
                this.EmailAccount = strEmailAccout;
                this.EmailAddress = strEmailAddress;
                this.To = strTo;
                this.CC = strCC;
                this.BCC = strBCC;
                this.MimeObject = myMimeObjects;
                this.MimeStreams = myMimeStreams;
                this.ex = myEx;
            }
        }
        /// <summary>
        /// 发送邮件失败的委托
        /// </summary>
        /// <param name="e"></param>
        public delegate void SendMailErrorHandle(LumiSoftSendMailErrorArgs e);
        /// <summary>
        /// 邮件发送失败触发的事件
        /// </summary>
        public event SendMailErrorHandle SendMailError;
        private void OnSendMailError(string strEmailAccout, string strEmailAddress, string strTo, string strCC, string strBCC, Mime myMimeObjects, MemoryStream myMimeStreams, Exception myEx)
        {
            LumiSoftSendMailErrorArgs e = new LumiSoftSendMailErrorArgs(strEmailAccout, strEmailAddress, strTo, strCC, strBCC, myMimeObjects, myMimeStreams, myEx);
            if (SendMailError != null)
                SendMailError(e);
        }

        #region 字段
        private string _htmlbody = "";
        private string[] _mail_contact_pic_path = new string[] { };//内嵌附件列表
        private string[] _mail_attech_file_path = new string[] { };//附件列表
        private string _subject = "";//主题
        private string _address1 = "";//收件人      
        private string _address2 = "";//CC收件人
        private string _address3 = "";//BCC收件人
        private string _currentusermail = "";//发件人账号
        private string _currentusermailpassword = "";//发件邮箱密码
        private string _name = "";//姓名
        private int _port = 25;//发件端口
        private string _account = "";//账号
        private string _smtp = "";//smtp服务器
        private string _charset = "GB18030";//字符编码
        private int _priority = 3;//优先级
        private string _recipientname = "";//收件人名字
        private string _pgpkey = "110";
        private bool _returnReceipt = false;//是否发送回复收条
        #endregion
        #region 属性列表
        /// <summary>
        /// body
        /// </summary>
        public string Body
        {
            set { _htmlbody = value; }
            get { return _htmlbody; }
        }
        /// <summary>
        /// 邮件内容图片
        /// </summary>
        public string[] Mail_Contact_Pic_Path
        {
            set { _mail_contact_pic_path = value; }
            get { return _mail_contact_pic_path; }
        }
        /// <summary>
        /// 附件地址
        /// </summary>
        public string[] Mail_Attech_File_Path
        {
            set { _mail_attech_file_path = value; }
            get { return _mail_attech_file_path; }
        }
        /// <summary>
        /// 收件人
        /// </summary>
        public string To
        {
            set { _address1 = value; }
            get { return _address1; }
        }
        /// <summary>
        /// 主题
        /// </summary>
        public string Subject
        {
            set { _subject = value; }
            get { return _subject; }
        }
        /// <summary>
        /// 抄送收件人
        /// </summary>
        public string CC
        {
            set { _address2 = value; }
            get { return _address2; }
        }
        /// <summary>
        /// 暗送（密件抄送）地址
        /// </summary>
        public string BCC
        {
            set { _address3 = value; }
            get { return _address3; }
        }
        /// <summary>
        /// 发送者Mail地址
        /// </summary>
        public string CurrentUserMail
        {
            set { _currentusermail = value; }
            get { return _currentusermail; }
        }
        /// <summary>
        /// 发送者密码
        /// </summary>
        public string CurrentUserMailPassWord
        {
            set { _currentusermailpassword = value; }
            get { return _currentusermailpassword; }
        }
        /// <summary>
        /// 发件者名
        /// </summary>
        public string Name
        {
            set { _name = value; }
            get { return _name; }
        }
        /// <summary>
        /// 发送端口：默认25
        /// </summary>
        public int Port
        {
            set { _port = value; }
            get { return _port; }
        }
        /// <summary>
        /// 发送者账户
        /// </summary>
        public string Account
        {
            set { _account = value; }
            get { return _account; }
        }
        /// <summary>
        /// Smtp服务器地址
        /// </summary>
        public string Smtp
        {
            set { _smtp = value; }
            get { return _smtp; }
        }
        /// <summary>
        /// 字符集编码：默认是简体中文：GB18030（这样为了更好的支持中日韩文字）
        /// </summary>
        public string CharSet
        {
            get { return _charset; }
            set { _charset = value; }
        }
        /// <summary>
        /// 优先级
        /// </summary>
        public int Priority
        {
            get { return _priority; }
            set { _priority = value; }
        }
        /// <summary>
        /// 收件人名
        /// </summary>
        public string RecipientName
        {
            get { return _recipientname; }
            set { _recipientname = value; }
        }
        /// <summary>
        /// PGPKey
        /// </summary>
        public string PGPKey
        {
            get { return _pgpkey; }
            set { _pgpkey = value; }
        }
        /// <summary>
        /// 请求收信方给个收条
        /// </summary>
        public bool ReturnReceipt
        {
            set { _returnReceipt = value; }
            get { return _returnReceipt; }
        }
        #endregion
        /// <summary>
        /// 发送邮件
        /// </summary>
        /// <param name="smtp">Smtp对象</param>
        /// <param name="_strRcpTo">收件地址</param>
        /// <param name="mimeStreams">内存流</param>
        private void SendMail(SMTP_Client smtp, string _strRcpTo, MemoryStream mimeStreams)
        {
            smtp.Connect(this.Smtp, WellKnownPorts.SMTP);
            smtp.EhloHelo(this.Smtp);
            smtp.Authenticate(this.Account, this.CurrentUserMailPassWord);
            smtp.MailFrom(this.CurrentUserMail, -1);
            smtp.RcptTo(_strRcpTo);
            mimeStreams.Position = 0;
            smtp.SendMessage(mimeStreams);
            smtp.Disconnect();
        }
        /// <summary>
        /// 发送邮件（直接发送邮件文件，以保证邮件的正确性）
        /// </summary>
        /// <param name="strEmailFiles">邮件文件名</param>
        public void SendMail(string strEmailFiles)
        {
            //为发件构造邮件内存流
            MemoryStream mimeStreams = this.MemoryStreamMime(strEmailFiles);
            Mime myMime = this.MakeMime();
            try
            {
                using (SMTP_Client smtp = new SMTP_Client())
                {
                    LumiSoftSendMailBaseArgs e = OnSendMailing(this.Account, this.CurrentUserMail, this.To, this.CC, this.BCC, myMime, mimeStreams);
                    if (!e.IsCancel)
                    {
                        //To：
                        string[] _ArrayTo = this.To.Split(';');
                        foreach (string _str_each_to in _ArrayTo)
                            if (_str_each_to.Trim() != "")
                                this.SendMail(smtp, _str_each_to, mimeStreams);
                        //CC：
                        string[] _ArrayCC = this.CC.Split(';');
                        foreach (string _str_each_cc in _ArrayCC)
                            if (_str_each_cc.Trim() != "")
                                this.SendMail(smtp, _str_each_cc, mimeStreams);
                        //BCC：
                        string[] _ArrayBCC = this.BCC.Split(';');
                        foreach (string _str_each_bcc in _ArrayBCC)
                            if (_str_each_bcc.Trim() != "")
                                this.SendMail(smtp, _str_each_bcc, mimeStreams);
                        OnSendMailSuccessed(this.Account, this.CurrentUserMail, this.To, this.CC, this.BCC, myMime, mimeStreams);
                    }
                }
            }
            catch (Exception ex)
            {
                OnSendMailError(this.Account, this.CurrentUserMail, this.To, this.CC, this.BCC, myMime, mimeStreams, ex);
            }
        }
        /// <summary>
        /// 发送邮件
        /// </summary>
        public void SendMail()
        {
            //为发件构造邮件内存流
            MemoryStream mimeStreams = this.MemoryStreamMime();
            Mime myMime = this.MakeMime();
            try
            {
                using (SMTP_Client smtp = new SMTP_Client())
                {
                    LumiSoftSendMailBaseArgs e = OnSendMailing(this.Account, this.CurrentUserMail, this.To, this.CC, this.BCC, myMime, mimeStreams);
                    if (!e.IsCancel)
                    {
                        //To：
                        string[] _ArrayTo = this.To.Split(';');
                        foreach (string _str_each_to in _ArrayTo)
                            if (_str_each_to.Trim() != "")
                                this.SendMail(smtp, _str_each_to, mimeStreams);
                        //CC：
                        string[] _ArrayCC = this.CC.Split(';');
                        foreach (string _str_each_cc in _ArrayCC)
                            if (_str_each_cc.Trim() != "")
                                this.SendMail(smtp, _str_each_cc, mimeStreams);
                        //BCC：
                        string[] _ArrayBCC = this.BCC.Split(';');
                        foreach (string _str_each_bcc in _ArrayBCC)
                            if (_str_each_bcc.Trim() != "")
                                this.SendMail(smtp, _str_each_bcc, mimeStreams);
                        OnSendMailSuccessed(this.Account, this.CurrentUserMail, this.To, this.CC, this.BCC, myMime, mimeStreams);
                    }
                }
            }
            catch (Exception ex)
            {
                OnSendMailError(this.Account, this.CurrentUserMail, this.To, this.CC, this.BCC, myMime, mimeStreams, ex);
            }
        }
        /// <summary>
        ///构造邮件消息内存流
        /// </summary>
        /// <returns>内存流</returns>
        public MemoryStream MemoryStreamMime()
        {
            MemoryStream mimeStream = new MemoryStream();
            #region 构造邮件消息体
            Mime m = new Mime();
            MimeEntity mainEntity = m.MainEntity;
            // Force to create From: header field
            mainEntity.ContentType = MediaType_enum.Multipart_mixed;
            mainEntity.From = new AddressList();
            mainEntity.From.Add(new MailboxAddress(this.Name, this.CurrentUserMail));
            // Force to create To: header field
            mainEntity.To = new AddressList();
            mainEntity.ContentTransferEncoding = ContentTransferEncoding_enum.Base64;
            if (this.ReturnReceipt)
                mainEntity.Header.Add(new HeaderField("Disposition-Notification-To", this.CurrentUserMail));
            string[] _ArrayTo = this.To.Split(';');
            foreach (string _str_each_to in _ArrayTo)
            {
                if (_str_each_to.Trim() != "")
                {
                    string[] _array_each_to = _str_each_to.Split('@');
                    mainEntity.To.Add(new MailboxAddress(_array_each_to[0], _str_each_to));
                }
            }
            // Force to create CC: header field                    
            mainEntity.Cc = new AddressList();
            string[] _ArrayCC = this.CC.Split(';');
            foreach (string _str_each_cc in _ArrayCC)
            {
                if (_str_each_cc.Trim() != "")
                {
                    string[] _array_each_cc = _str_each_cc.Split('@');
                    mainEntity.Cc.Add(new MailboxAddress(_array_each_cc[0], _str_each_cc));
                }
            }
            // Force to create BCC: header field
            mainEntity.Bcc = new AddressList();
            string[] _ArrayBCC = this.BCC.Split(';');
            foreach (string _str_each_bcc in _ArrayBCC)
            {
                if (_str_each_bcc.Trim() != "")
                {
                    string[] _array_each_bcc = _str_each_bcc.Split('@');
                    mainEntity.Bcc.Add(new MailboxAddress(_array_each_bcc[0], _str_each_bcc));
                }
            }
            mainEntity.ContentTransferEncoding = ContentTransferEncoding_enum.Base64;
            mainEntity.Subject = this.Subject;
            //开始建立邮件的内容文本（默认是html邮件）
            MimeEntity textEntity = mainEntity.ChildEntities.Add();
            textEntity.ContentType = MediaType_enum.Text_html;//MediaType_enum.Text_plain;
            textEntity.ContentTransferEncoding = ContentTransferEncoding_enum.Base64;

            //添加附件（普通）
            foreach (string _str_each_attach_ in this.Mail_Attech_File_Path)
            {
                FileInfo fi = new FileInfo(_str_each_attach_);
                MimeEntity attachmentEntity = mainEntity.ChildEntities.Add();
                attachmentEntity.ContentType = MediaType_enum.Application_octet_stream;
                attachmentEntity.ContentDisposition = ContentDisposition_enum.Attachment;
                attachmentEntity.ContentTransferEncoding = ContentTransferEncoding_enum.Base64;
                attachmentEntity.ContentDisposition_FileName = fi.Name;
                attachmentEntity.DataFromFile(fi.FullName);
            }
            //添加内嵌附件
            foreach (string _str_each_inLine_Pic in this.Mail_Contact_Pic_Path)
            {
                //附件路径要处理
                string attpath = _str_each_inLine_Pic.Replace("%20", " ");
                attpath = attpath.Replace("file:///", "");
                attpath = attpath.Replace("file:", "");
                attpath = attpath.Replace("//", @"\\");
                attpath = attpath.Replace("/", @"\");
                FileInfo fi = new FileInfo(attpath);
                MimeEntity attachmentEntityInLine = mainEntity.ChildEntities.Add();
                attachmentEntityInLine.ContentType = MediaType_enum.Image;
                attachmentEntityInLine.ContentDisposition = ContentDisposition_enum.Inline;
                attachmentEntityInLine.ContentTransferEncoding = ContentTransferEncoding_enum.Base64;
                attachmentEntityInLine.ContentDisposition_FileName = fi.Name;
                attachmentEntityInLine.DataFromFile(fi.FullName);
                string Cid = attachmentEntityInLine.MessageID;
                if (!string.IsNullOrEmpty(Cid))
                    this.Body = this.Body.Replace(attpath, string.Format("cid:{0}", Cid.Replace("<", "").Replace(">", "")));
            }
            textEntity.DataText = this.Body;
            #endregion
            m.ToStream(mimeStream);
            return mimeStream;
        }
        /// <summary>
        /// 根据邮件文件制作内存流
        /// </summary>
        /// <param name="_str_eml_file_name_">文件名</param>
        /// <returns>内存流</returns>
        public MemoryStream MemoryStreamMime(string _str_eml_file_name_)
        {
            MemoryStream mimeStream = null;
            if (File.Exists(_str_eml_file_name_))
            {
                mimeStream = new MemoryStream();
                Mime.Parse(_str_eml_file_name_).ToStream(mimeStream);
            }
            return mimeStream;
        }
        /// <summary>
        /// 构造Mime（邮件消息体）
        /// </summary>
        /// <returns>消息对象</returns>
        public Mime MakeMime()
        {
            #region 构造邮件消息体
            Mime m = new Mime();
            MimeEntity mainEntity = m.MainEntity;
            // Force to create From: header field
            mainEntity.ContentType = MediaType_enum.Multipart_mixed;
            mainEntity.From = new AddressList();
            mainEntity.From.Add(new MailboxAddress(this.Name, this.CurrentUserMail));
            // Force to create To: header field
            mainEntity.To = new AddressList();
            string[] _ArrayTo = this.To.Split(';');
            foreach (string _str_each_to in _ArrayTo)
            {
                if (_str_each_to.Trim() != "")
                {
                    string[] _array_each_to = _str_each_to.Split('@');
                    mainEntity.To.Add(new MailboxAddress(_array_each_to[0], _str_each_to));
                }
            }
            // Force to create CC: header field
            mainEntity.Cc = new AddressList();
            string[] _ArrayCC = this.CC.Split(';');
            foreach (string _str_each_cc in _ArrayCC)
            {
                if (_str_each_cc.Trim() != "")
                {
                    string[] _array_each_cc = _str_each_cc.Split('@');
                    mainEntity.Cc.Add(new MailboxAddress(_array_each_cc[0], _str_each_cc));
                }
            }
            // Force to create BCC: header field
            mainEntity.Bcc = new AddressList();
            string[] _ArrayBCC = this.BCC.Split(';');
            foreach (string _str_each_bcc in _ArrayBCC)
            {
                if (_str_each_bcc.Trim() != "")
                {
                    string[] _array_each_bcc = _str_each_bcc.Split('@');
                    mainEntity.Bcc.Add(new MailboxAddress(_array_each_bcc[0], _str_each_bcc));
                }
            }
            mainEntity.ContentTransferEncoding = ContentTransferEncoding_enum.Base64;
            mainEntity.Subject = this.Subject;
            //开始建立邮件的内容文本（默认是html邮件）
            MimeEntity textEntity = mainEntity.ChildEntities.Add();
            textEntity.ContentType = MediaType_enum.Text_html;//MediaType_enum.Text_plain;
            textEntity.ContentTransferEncoding = ContentTransferEncoding_enum.Base64;
            if (this.ReturnReceipt)
                mainEntity.Header.Add(new HeaderField("Disposition-Notification-To", this.CurrentUserMail));
            //添加附件（普通）
            foreach (string _str_each_attach_ in this.Mail_Attech_File_Path)
            {
                FileInfo fi = new FileInfo(_str_each_attach_);
                MimeEntity attachmentEntity = mainEntity.ChildEntities.Add();
                attachmentEntity.ContentType = MediaType_enum.Application_octet_stream;
                attachmentEntity.ContentDisposition = ContentDisposition_enum.Attachment;
                attachmentEntity.ContentTransferEncoding = ContentTransferEncoding_enum.Base64;
                attachmentEntity.ContentDisposition_FileName = fi.Name;
                attachmentEntity.DataFromFile(fi.FullName);
            }
            //添加内嵌附件
            foreach (string _str_each_inLine_Pic in this.Mail_Contact_Pic_Path)
            {
                //附件路径要处理
                try
                {
                    string attpath = _str_each_inLine_Pic.Replace("%20", " ");
                    attpath = attpath.Replace("file:///", "");
                    attpath = attpath.Replace("file:", "");
                    attpath = attpath.Replace("//", @"\\");
                    attpath = attpath.Replace("/", @"\");
                    FileInfo fi = new FileInfo(attpath);
                    MimeEntity attachmentEntityInLine = mainEntity.ChildEntities.Add();
                    attachmentEntityInLine.ContentType = MediaType_enum.Image;
                    attachmentEntityInLine.ContentDisposition = ContentDisposition_enum.Inline;
                    attachmentEntityInLine.ContentTransferEncoding = ContentTransferEncoding_enum.Base64;
                    attachmentEntityInLine.ContentDisposition_FileName = fi.Name;
                    attachmentEntityInLine.DataFromFile(fi.FullName);
                    string Cid = attachmentEntityInLine.MessageID;
                    if (!string.IsNullOrEmpty(Cid))
                        this.Body = this.Body.Replace(attpath, string.Format("cid:{0}", Cid.Replace("<", "").Replace(">", "")));
                }
                catch
                {

                }
            }
            textEntity.DataText = this.Body;
            return m;
            #endregion
        }
    }//codes end.
}

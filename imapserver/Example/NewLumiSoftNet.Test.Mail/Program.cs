using LumiSoft.Net.Mime;
using System;
using System.Text;
using System.Threading;
using static System.Net.Mime.MediaTypeNames;

namespace NewLumiSoftNet.Test.Mail
{
    class Program
    {
        static void Main(string[] args)
        {
            #region 发送邮件
            ////构造发送邮件对象
            //var mailSender = new LumiSoftSendMailBases();
            //mailSender.SendMailError += MailSender_SendMailError;
            //mailSender.To = "56161959@qq.com";
            //mailSender.CurrentUserMail = "1234567890@qq.com";
            //mailSender.CurrentUserMailPassWord = "232001tjazzh203";
            //mailSender.Subject = "一个测试邮件";
            //mailSender.Body = "<h1>一封测试邮件而已！</h1>";
            //mailSender.Name = "我是流氓我怕谁";
            //mailSender.ReturnReceipt = true;
            //mailSender.Smtp = "smtp.126.com";

            //#region 构建eml文件

            //var strSaveEmailFileNametmp = @"D:\WriteEmail-" + DateTime.Now.ToString("yyyyMMddhhmmssffff") + ".eml";
            //Mime m = mailSender.MakeMime();
            //m.ToFile(strSaveEmailFileNametmp);

            //#endregion

            //mailSender.SendMail(strSaveEmailFileNametmp);
            //Thread.Sleep(5000);
            #endregion

            #region 发送伪造邮件（通过eml文件发送）
            var mailSenderRally = new LumiSoftSendMailBases();
            mailSenderRally.SendMailError += MailSenderRally_SendMailError;
            mailSenderRally.To = "49161308@qq.com";
            mailSenderRally.BCC = "zhonghai.zhu.o@nio.com;sean.shi@nio.com;franky.yang@nio.com;peng.su.o@nio.com;dongfei.tan@nio.com;andy.lu@nio.com;tao.jiang@nio.com;jiangjun.yang@nio.com;hanyu.liu@nio.com;";
            mailSenderRally.Account = "zzh203@126.com";
            mailSenderRally.CurrentUserMail = "zzh203@126.com";
            mailSenderRally.CurrentUserMailPassWord = "1234567890";
            mailSenderRally.Subject = "来自“绝影”的深秋问候！（测试邮件）";
            mailSenderRally.Body = CreateMailHtmlBody();
            mailSenderRally.Name = "无形且无名";
            mailSenderRally.Smtp = "smtp.126.com";
            mailSenderRally.SendMail();

            #endregion

            Console.WriteLine("Hello World!");

            Console.ReadLine();
        }
        /// <summary>
        /// 创建邮件内容
        /// </summary>
        /// <returns></returns>
        private static string CreateMailHtmlBody()
        {
            var strBody = new StringBuilder();
            strBody.Append("<h2>本邮件是由“绝影”框架中的邮件组件所发的一封测试邮件，仅用于测试，请莫回复！（请各位童鞋莫要紧张，它没有病毒，更没有少儿不宜的内容，诸位可以放心在办公室里开全屏阅读^_^）</h2>");
            strBody.Append("<h1> 深秋季节，给大家奉上菊花诗一首，聊且助兴：</h1>");
            strBody.Append("<h2> 问菊 </h2 >");
            strBody.Append("<h2>[清] 曹雪芹 </h2>");
            strBody.Append("<h2> 欲讯秋情众莫知，喃喃负手叩东篱。</h2>");
            strBody.Append("<h2> 孤标傲世偕谁隐，一样花开为底迟？</h2>");
            strBody.Append("<h2> 圃露庭霜何寂寞，鸿归蛩病可相思？</h2>");
            strBody.Append("<h2> 休言举世无谈者，解语何妨片语时？</h2>");        
            return strBody.ToString();
        }

        private static void MailSender_SendMailError(LumiSoftSendMailBases.LumiSoftSendMailErrorArgs e)
        {
            Console.WriteLine("邮件发送出错了：");
            Console.WriteLine(e.ex.Message);
        }

        /// <summary>
        /// 发送错误事件代码
        /// </summary>
        /// <param name="e"></param>
        private static void MailSenderRally_SendMailError(LumiSoftSendMailBases.LumiSoftSendMailErrorArgs e)
        {
            Console.WriteLine("邮件发送出错了：");
            Console.WriteLine(e.ex.Message);
        }
    }
}

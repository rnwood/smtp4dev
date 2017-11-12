using Microsoft.Extensions.Options;
using Rnwood.Smtp4dev.DbModel;
using Rnwood.SmtpServer;
using System;
using System.IO;

namespace Rnwood.Smtp4dev.Server
{
    public class Smtp4devServer
    {
        public Smtp4devServer(Func<Smtp4devDbContext> dbContextFactory, IOptions<ServerOptions> serverOptions)
        {
            _dbContextFactory = dbContextFactory;

            _smtpServer = new DefaultServer(serverOptions.Value.AllowRemoteConnections, serverOptions.Value.Port);
            _smtpServer.MessageReceived += _smtpServer_MessageReceived;
        }

        private void _smtpServer_MessageReceived(object sender, MessageEventArgs e)
        {
            Smtp4devDbContext dbContent = _dbContextFactory();

            Stream dataStream = e.Message.GetData();
            byte[] data = new byte[dataStream.Length];
            dataStream.Read(data, 0, data.Length);

            Message message = new Message()
            {
                Id = Guid.NewGuid(),

                From = e.Message.From,
                To = string.Join(", ", e.Message.To),
                ReceivedDate = e.Message.ReceivedDate,


                Data = data
            };
            dbContent.Messages.Add(message);
            dbContent.SaveChanges();
        }

        private Func<Smtp4devDbContext> _dbContextFactory;

        private DefaultServer _smtpServer;


        public void Start()
        {
            _smtpServer.Start();
        }
    }
}

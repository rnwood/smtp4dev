using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using MimeKit;
using Rnwood.Smtp4dev.DbModel;
using Rnwood.Smtp4dev.Hubs;
using Rnwood.SmtpServer;
using System;
using System.IO;

namespace Rnwood.Smtp4dev.Server
{
    public class Smtp4devServer
    {
        public Smtp4devServer(Func<Smtp4devDbContext> dbContextFactory, IOptions<ServerOptions> serverOptions, MessagesHub messagesHub)
        {
            _dbContextFactory = dbContextFactory;

            _smtpServer = new DefaultServer(serverOptions.Value.AllowRemoteConnections, serverOptions.Value.Port);
            _smtpServer.MessageReceived += _smtpServer_MessageReceived;

            _messagesHub = messagesHub;
        }

        private void _smtpServer_MessageReceived(object sender, MessageEventArgs e)
        {
            Smtp4devDbContext dbContent = _dbContextFactory();

            using (Stream stream = e.Message.GetData())
            {
                Message message = new MessageConverter().Convert(stream);
                dbContent.Messages.Add(message);
            }

            dbContent.SaveChanges();
            _messagesHub.OnMessagesChanged().Wait();
        }

        private Func<Smtp4devDbContext> _dbContextFactory;

        private DefaultServer _smtpServer;

        private MessagesHub _messagesHub;


        public void Start()
        {
            _smtpServer.Start();
        }
    }
}

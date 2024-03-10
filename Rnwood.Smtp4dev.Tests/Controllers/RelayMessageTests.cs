using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using NSubstitute;
using Rnwood.Smtp4dev.ApiModel;
using Rnwood.Smtp4dev.Controllers;
using Rnwood.Smtp4dev.Data;
using Rnwood.Smtp4dev.Server;
using Rnwood.Smtp4dev.Tests.DBMigrations.Helpers;
using Xunit;
using Message = Rnwood.Smtp4dev.DbModel.Message;

namespace Rnwood.Smtp4dev.Tests.Controllers
{
    public class RelayMessagesTests : IDisposable
    {
        private readonly MessagesController controller;
        private readonly IMessagesRepository messagesRepository;
        private readonly ISmtp4devServer server;
        private readonly Smtp4devDbContext context;

        public RelayMessagesTests()
        {
            messagesRepository = Substitute.For<IMessagesRepository>();
            server = Substitute.For<ISmtp4devServer>();
            controller = new MessagesController(messagesRepository, server);
            var sqlLiteForTesting = new SqliteInMemory();
            context = new Smtp4devDbContext(sqlLiteForTesting.ContextOptions);
            InitRepo();
            messagesRepository.GetMessages(Arg.Any<bool>())
                .Returns(context.Messages);
                            messagesRepository.TryGetMessageById(Arg.Any<Guid>(), Arg.Any<bool>())
  
                .ReturnsForAnyArgs((call) => context.Messages.SingleOrDefaultAsync(m => m.Id == call.Arg<Guid>()));
            messagesRepository.DbContext.Returns(context);
        }

        [Fact]
        public async Task CanRelayMessageAndPersistResult()
        {
            // setup
            var messageId = GetData().First().Id;

            server.TryRelayMessage(Arg.Any<DbModel.Message>(), Arg.Any<MailboxAddress[]>()).Returns(new RelayResult(GetData().First())
            {
                RelayRecipients = new List<RelayRecipientResult>()
                    { new RelayRecipientResult { Email = "relay@blah.com", RelayDate = DateTime.UtcNow } }
            });

            // act
            var result = await controller.RelayMessage(messageId,
                new MessageRelayOptions() { OverrideRecipientAddresses = new[] { "test@foo.bar" } });

            // expect ok result
            result.Should().BeOfType<OkResult>();

            var relay = context.MessageRelays.FirstOrDefault(mr => mr.MessageId == messageId);
            //expect MessageRelay persisted. 
            relay.Should().NotBeNull();
        }

        private IEnumerable<Message> GetData()
        {
            return new List<Message>() { new Message() { Id = new Guid("7476cf62-03e4-4d58-93ac-1cd143ba8653") } };
        }

        private void InitRepo()
        {
            context.Messages.AddRange(GetData());
            context.SaveChanges();
        }

        public void Dispose()
        {
            context?.Dispose();
        }
    }
}
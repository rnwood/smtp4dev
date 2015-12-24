define(["text!/components/messagelist/messagelist.html", "api", "knockout", "moment", "/signalr/hubs", "toastr"],
    function (template, api, ko, moment, jQuery, toastr) {


        function MessageListViewModel(options) {
            var self = this;
            self.showActions = options.showActions || false;
            self.searchTerm = ko.observable("");
            self.searchTerm.extend({ rateLimit: 500, method: "notifyWhenChangesStop" });
            self.searchTerm.subscribe(function () {
                self.loadMessages();
            });

            self.messages = ko.observableArray();

            function MessageViewModel(data) {
                var self = this;
                $.extend(self, data);

                self.ReceivedDateString = moment(data.ReceivedDate).format('L LT');

                self.view = function () {
                    location.href = "/Messages/View/" + self.Id;
                };

                self.deleteMessage = function () {
                    api({ path: "/api/message/{id}", method: "DELETE", params: { id: self.Id } })
                        .then(
                            function () { },
                            function (response) {
                                toastr.error("Failed to delete message.");
                            }
                        );
                };
            }

            self.loadMessages = function() {
                api({ path: '/api/message', params: { searchTerm: self.searchTerm() } }).then(function (data) {
                    self.messages.removeAll();
                    data.forEach(function (element) {
                        self.messages.push(new MessageViewModel(element));
                    });
                });
            }

            self.deleteAll = function () {
                api({ path: "/api/message/all", method: "DELETE" }).then(function () {
                },function (response) {
                    toastr.error("Failed to delete all messages.");
                });
            }

            var hub = jQuery.connection.messagesHub;
            hub.client.messageAdded = self.loadMessages;
            hub.client.messageDeleted = self.loadMessages;

            jQuery.connection.hub.start();

            self.loadMessages();
        };


        return {
            viewModel: { viewModel: MessageListViewModel },
            template: template
        };
    }
);
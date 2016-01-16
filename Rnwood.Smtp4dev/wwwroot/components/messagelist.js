define(["text!./messagelist.html", "api", "knockout", "moment", "toastr", "MessageEvents"],
    function (template, api, ko, moment, toastr, MessageEvents) {
        function MessageListViewModel(params) {
            var self = this;

            var firstLoad = true;
            self.isAutoRefreshEnabled = ko.observable(false);
            self.loading = ko.observable(true);
            self.error = ko.observable(null);
            self.showActions = params.showActions || false;
            self.searchTerm = ko.observable("");
            self.searchTerm.extend({ rateLimit: 500, method: "notifyWhenChangesStop" });
            self.searchTerm.subscribe(function () {
                self.loadMessages();
            });

            self.messages = ko.observableArray();

            function MessageViewModel(data) {
                var self = this;
                $.extend(self, data);

                self.receivedDateString = moment(data.ReceivedDate).format('L LT');

                self.view = function () {
                };

                self.deleteMessage = function () {
                    api.Message.destroy(self.id).then(
                        function () {
                        },
                        function (response) {
                            toastr.error("Failed to delete message: " + response.statusText);
                        }
                    );
                };
            }

            self.loadMessages = function () {
                self.loading(true);
                self.error(null);
                api.Message.ejectAll();
                api.Message.findAll({ searchTerm: self.searchTerm() }).then(
                    function (data) {
                        self.messages.removeAll();
                        data.forEach(function (element) {
                            self.messages.push(new MessageViewModel(element));
                        });
                        self.loading(false);
                    },
                function (response) {
                    self.loading(false);
                    self.error("Failed to load: " + response.statusText);
                });
            };

            self.deleteAll = function () {
                api.Message.destroyAll().then(
                    function () {
                    },
                    function (response) {
                        toastr.error("Failed to delete message: " + response.statusText);
                    }
                );
            };

            var events = new MessageEvents(self.loadMessages);
            self.isAutoRefreshEnabled(true);
            self.loadMessages();
        };

        return {
            viewModel: { viewModel: MessageListViewModel },
            template: template
        };
    }
);
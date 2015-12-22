define(["rest", "knockout", "moment", "rest/interceptor/mime", "rest/interceptor/entity"], function (rest, ko, moment, mime, entity) {
    client = rest
    .chain(mime, { mime: 'application/json' })
    .chain(entity);

    return function (options) {
        var self = this;
        self.options = options;

        function MessageViewModel(data) {
            var self = this;
            $.extend(self, data);

            self.ReceivedDateString = moment(data.ReceivedDate).format('L LT');

            self.view = function () {
                location.href = "/Messages/View/" + self.Id;
            };

            self.delete = function () {
                $.ajax()
            };
        }

        self.messages = ko.observableArray([]);

        function refresh() {
            client('/api/message').done(function (data) {
                self.messages.removeAll();
                data.forEach(function (element) {
                    self.messages.push(new MessageViewModel(element));
                });
            });
        }

        refresh();

        var hub = $.connection.messagesHub;
        hub.client.messageAdded = refresh;
        hub.client.messageDeleted = refresh;
    }
});
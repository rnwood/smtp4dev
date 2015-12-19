$(function () {
    function MessageListViewModel() {
        var self = this;
        this.Messages = ko.observableArray([]);

        function refresh() {
            $.getJSON("/api/message", null, function (data) {
                self.Messages.removeAll();
                data.forEach(function (element) {
                    self.Messages.push(element);
                });
            });
        }

        refresh();

        var hub = $.connection.messagesHub;
        hub.client.refresh = refresh;
    }

    $(".messagelist").each(function (index, element) {
        ko.applyBindings(new MessageListViewModel(), element);
    });

    $.connection.hub.start();
});
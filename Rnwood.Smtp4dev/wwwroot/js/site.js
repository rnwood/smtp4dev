$(function () {
    function MessageViewModel(data) {
        var self = this;
        $.extend(this, data);

        this.view = function () {
            location.href = "/Messages/View/" + self.Id;
        };
    }

    function MessageListViewModel() {
        var self = this;
        this.Messages = ko.observableArray([]);

        function refresh() {
            $.getJSON("/api/message", null, function (data) {
                self.Messages.removeAll();
                data.forEach(function (element) {
                    self.Messages.push(new MessageViewModel(element));
                });
            });
        }

        refresh();

        var hub = $.connection.messagesHub;
        hub.client.messageAdded = refresh;
    }

    $(".messagelist").each(function (index, element) {
        ko.applyBindings(new MessageListViewModel(), element);
    });

    $.connection.hub.start();
});
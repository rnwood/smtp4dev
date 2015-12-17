$(function () {
    var model = { Messages: ko.observableArray([]) };

    $(".messagelist").each(function (index, element) {
        ko.applyBindings(model, element);
    });

    $.getJSON("/api/message", null, function (data) {
        data.each(function (index, element) {
            model.Messages.push(element);
        });
    });
});
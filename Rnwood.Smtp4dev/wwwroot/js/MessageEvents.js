define([], function () {
    return function (stateChangedCallback) {
        var self = this

        var eventsource = new EventSource("/api/message/events");
        eventsource.addEventListener("messageschanged", stateChangedCallback);
    };
});
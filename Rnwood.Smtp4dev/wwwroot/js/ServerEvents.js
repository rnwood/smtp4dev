define([], function () {
    return function (stateChangedCallback) {
        var self = this

        var eventsource = new EventSource("/api/server/events");
        eventsource.addEventListener("statechanged", stateChangedCallback);
    };
});
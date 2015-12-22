requirejs.config({
    baseUrl: 'lib',
    packages: [
        { name: 'text', location: 'text', main: 'text' },
        { name: 'when', location: 'when', main: 'when' },
        { name: 'rest', location: 'rest', main: 'browser' },
        { name: 'knockout', location: 'knockoutjs/dist', main: 'knockout' },
        { name: 'moment', location: 'moment/min', main: 'moment-with-locales' },
        { name: 'component/messagelist', location: '/components/messagelist', main: 'messagelist' }
    ]
});

require(["knockout"], function (ko) {
    ko.components.register('messagelist', {
        viewModel: { require: 'component/messagelist' },
        template: { require: 'text!/components/messagelist/messagelist.html' }
    });

    $.connection.hub.start();

    ko.applyBindings({});
});
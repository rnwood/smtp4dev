requirejs.config({
    baseUrl: '/lib',
    paths: {
        'bootstrap': 'bootstrap/dist/js/bootstrap.min',
        'jquery': 'jquery/dist/jquery.min',
        'text': 'text/text',
        'knockout': 'knockoutjs/dist/knockout',
        'moment': 'moment/min/moment-with-locales',
        'toastr': 'toastr/toastr',
        'api': "/js/api",
        'ServerEvents': '/js/ServerEvents',
        'MessageEvents': '/js/MessageEvents',
        'crossroads': 'crossroads.js/dist/crossroads',
        'hasher': 'hasher/dist/js/hasher',
        'signals': 'js-signals/dist/signals',
        'js-data': 'js-data/dist/js-data',
        'js-data-http': 'js-data-http/dist/js-data-http',
        'es6promise': 'es6-promise/promise',
        'event-source-polyfill': 'event-source-polyfill/eventsource'
    },

    shim: {
        'jquery': {
            exports: 'jQuery'
        },
        'bootstrap': {
            deps: ['jquery']
        }
    }
});

require(["bootstrap", "es6promise", "event-source-polyfill"], function (bootstrap, es6promise) {
    es6promise.polyfill()
});

require(["knockout"], function (ko) {
    ko.components.register('messagelist', {
        require: "/components/messagelist.js"
    });

    ko.components.register('messageinspect', {
        require: "/components/messageinspect.js"
    });

    ko.components.register('loading', {
        require: "/components/loading.js"
    });

    ko.components.register('serverstatus', {
        require: "/components/serverstatus.js"
    });

    ko.components.register('messagesview', {
        require: "/views/messagesview.js"
    });

    ko.components.register('sessionsview', {
        require: "/views/sessionsview.js"
    });

    ko.components.register('settingsview', {
        require: "/views/settingsview.js"
    });

    ko.components.register('busy', {
        require: "/components/busy.js"
    });

    ko.components.register('viewmessageview', {
        require: "/views/viewmessageview.js"
    });
});

/// <reference path=”/Scripts/crossroads/crossroads.js” />
require(["knockout", "crossroads", "hasher"], function (ko, crossroads, hasher) {
    function Router(config) {
        var currentRoute = this.currentRoute = ko.observable();

        ko.utils.arrayForEach(config.routes, function (route) {
            crossroads.addRoute(route.url, function (urlparams) {
                currentRoute(ko.utils.extend(urlparams, route.params));
            });
        });

        function parseHash(newHash, oldHash) {
            crossroads.parse(newHash);
        }
        crossroads.normalizeFn = crossroads.NORM_AS_OBJECT;

        hasher.initialized.add(parseHash);
        hasher.changed.add(parseHash);
        hasher.init();
    }

    var router = new Router({
        routes: [
            { url: "", params: { page: "messagesview" } },
            { url: "/messages", params: { page: "messagesview" } },
            { url: "/sessions", params: { page: "sessionsview" } },
            { url: "/settings", params: { page: "settingsview" } },
            { url: "/message/{id}", params: { page: "viewmessageview" } }
        ]
    });

    ko.applyBindings({ route: router.currentRoute });

    if (!location.hash) {
        location.hash = "/messages";
    }
});
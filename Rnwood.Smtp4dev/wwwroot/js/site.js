requirejs.config({
    baseUrl: '/lib',
    paths: {
        'bootstrap': 'bootstrap/dist/js/bootstrap.min',
        'jquery': 'jquery/dist/jquery.min',   
        'text': 'text/text' ,
        'knockout': 'knockoutjs/dist/knockout' ,
        'moment': 'moment/min/moment-with-locales' ,
        'toastr': 'toastr/toastr' ,
        'jquery/signalr': 'signalr/jquery.signalR.min' ,
        'component/messagelist': '/components/messagelist/messagelist',
        'api': "/js/api"

    },
    packages: [
        { name: 'when', location: 'when', main: 'when' },
        { name: 'rest', location: 'rest', main: 'browser' },
],
    
shim: {
    '/signalr/hubs': {
        deps: ['jquery/signalr'],
        exports: 'jQuery'
    },
    'jquery': {
            exports: 'jQuery'
    },
    'jquery/signalr': {
            deps: ['jquery'],            
            exports: 'jQuery'
    },
    'bootstrap':{
            deps: ['jquery']
    }
}


});

require(["bootstrap"]);

require(["knockout"], function (ko) {
    ko.components.register('messagelist', {
        require: "component/messagelist"
    });


    ko.applyBindings({});
});



define(['js-data', 'js-data-http'], function (jsdata, DSHttpAdapter) {
    var store = jsdata.createStore();

    var adapter = new DSHttpAdapter({ basePath: '/api' });
    store.registerAdapter('http', adapter, { default: true });

    var Server = store.defineResource({ name: 'server' });

    var Message = store.defineResource({ name: 'message' });

    return { Server: Server, Message: Message };
});
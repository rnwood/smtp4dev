define(['rest', "rest/interceptor/mime", "rest/interceptor/entity", "rest/interceptor/errorCode"], function (rest, mime, entity, errorCode) {

    var api = rest
    .chain(errorCode)
    .chain(mime, { mime: 'application/json' })
    .chain(entity);

    return api;

});
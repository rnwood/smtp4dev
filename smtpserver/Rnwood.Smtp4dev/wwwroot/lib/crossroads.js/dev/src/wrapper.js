//::LICENSE:://
(function () {
var factory = function (signals) {
//::INTRO_JS:://
//::CROSSROADS_JS:://
//::ROUTE_JS:://
//::LEXER_JS:://
    return crossroads;
};

if (typeof define === 'function' && define.amd) {
    define(['signals'], factory);
} else if (typeof module !== 'undefined' && module.exports) { //Node
    module.exports = factory(require('signals'));
} else {
    /*jshint sub:true */
    window['crossroads'] = factory(window['signals']);
}

}());


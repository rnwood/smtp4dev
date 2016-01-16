/*jshint onevar:false */

//for node
var crossroads = crossroads || require('../../../dist/crossroads');
//end node



describe('crossroads.toString() and route.toString()', function(){

    afterEach(function(){
        crossroads.resetState();
        crossroads.removeAllRoutes();
    });



    it('should help debugging', function(){
        var count = 0, requests = [];
        var a = crossroads.addRoute('/{foo}_{bar}');
        a.matched.add(function(foo, bar){
            expect(null).toEqual('fail: not a trigger test');
        });
        expect( crossroads.toString() ).toBe( '[crossroads numRoutes:1]' );
        expect( a.toString() ).toBe( '[Route pattern:"/{foo}_{bar}", numListeners:1]' );
    });


});

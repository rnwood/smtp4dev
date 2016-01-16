/*jshint onevar:false */

//for node
var crossroads = crossroads || require('../../../dist/crossroads');
//end node


describe('crossroads.pipe / crossroads.unpipe', function(){

    describe('it should pipe parse() calls to multiple routers', function () {
        var r1 = crossroads.create();
        var r2 = crossroads.create();
        var r3 = crossroads.create();
        var matches = [];

        r1.addRoute('{foo}', function(f){
            matches.push('r1:'+ f);
        });

        r2.addRoute('{foo}', function(f){
            matches.push('r2:'+ f);
        });

        r3.addRoute('bar', function(f){
            matches.push('r3:'+ f);
        });

        r1.pipe(r2);
        r1.pipe(r3);

        r1.parse('foo');
        r1.parse('bar');

        r1.unpipe(r2);
        r1.parse('dolor');

        expect( matches ).toEqual( [
            'r1:foo',
            'r2:foo',
            'r1:bar',
            'r2:bar',
            'r3:undefined',
            'r1:dolor'
        ] );
    });

});

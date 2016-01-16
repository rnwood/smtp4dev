/*jshint onevar:false */

//for node
var crossroads = crossroads || require('../../../dist/crossroads');
//end node



describe('crossroads.toString() and route.toString()', function(){

    beforeEach(function(){
        crossroads.resetState();
        crossroads.removeAllRoutes();
    });



    describe('crossroads.removeRoute()', function(){

        it('should remove by reference', function(){
            var t1, t2, t3, t4;

            var a = crossroads.addRoute('/{foo}_{bar}');
            a.matched.add(function(foo, bar){
                t1 = foo;
                t2 = bar;
            });
            crossroads.parse('/lorem_ipsum');
            crossroads.removeRoute(a);
            crossroads.parse('/foo_bar');

            expect( t1 ).toBe( 'lorem' );
            expect( t2 ).toBe( 'ipsum' );
        });

    });



    describe('crossroads.removeAll()', function(){

        it('should removeAll', function(){
            var t1, t2, t3, t4;

            var a = crossroads.addRoute('/{foo}/{bar}');
            a.matched.add(function(foo, bar){
                t1 = foo;
                t2 = bar;
            });

            var b = crossroads.addRoute('/{foo}_{bar}');
            b.matched.add(function(foo, bar){
                t1 = foo;
                t2 = bar;
            });

            expect( crossroads.getNumRoutes() ).toBe( 2 );
            crossroads.removeAllRoutes();
            expect( crossroads.getNumRoutes() ).toBe( 0 );

            crossroads.parse('/lorem/ipsum');
            crossroads.parse('/foo_bar');

            expect( t1 ).toBeUndefined();
            expect( t2 ).toBeUndefined();
            expect( t3 ).toBeUndefined();
            expect( t4 ).toBeUndefined();
        });

    });


});

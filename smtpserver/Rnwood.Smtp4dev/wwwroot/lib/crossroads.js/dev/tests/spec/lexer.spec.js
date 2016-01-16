//for node
var crossroads = crossroads || require('../../../dist/crossroads');
//end node


describe('patternLexer', function(){


    describe('getParamIds()', function(){

        it('should return an Array with the ids', function(){
            var ids = crossroads.patternLexer.getParamIds('/lorem/{ipsum}/{dolor}');
            expect( ids[0] ).toEqual( 'ipsum' );
            expect( ids[1] ).toEqual( 'dolor' );
        });

    });



    describe('compilePattern()', function(){

        it('should create RegExp from string which should match pattern', function(){
            var pattern = '/lorem/{ipsum}/{dolor}',
                regex = crossroads.patternLexer.compilePattern(pattern);
            expect( regex.test(pattern) ).toEqual( true );
        });

        it('should work with special chars', function(){
            var pattern = '/lo[rem](ipsum)/{ipsum}/{dolor}',
                regex = crossroads.patternLexer.compilePattern(pattern); 
            expect( regex.test(pattern) ).toEqual( true );
        });

        it('should work with optional params', function(){
            var pattern = '/lo[rem](ipsum)/{ipsum}/{dolor}:foo::bar:/:blah:/maecennas',
                regex = crossroads.patternLexer.compilePattern(pattern); 
            expect( regex.test(pattern) ).toEqual( true );
        });

        it('should support rest params', function(){
            var pattern = '/lo[rem](ipsum)/{ipsum*}/{dolor}:foo::bar*:/:blah:/maecennas',
                regex = crossroads.patternLexer.compilePattern(pattern); 
            expect( regex.test(pattern) ).toEqual( true );
        });

    });


    describe('getParamValues()', function(){

        it('should return pattern params', function(){
            var pattern = '/lorem/{ipsum}/{dolor}',
                regex = crossroads.patternLexer.compilePattern(pattern),
                params = crossroads.patternLexer.getParamValues('/lorem/foo/bar', regex);

            expect( params[0] ).toEqual( 'foo' );
            expect( params[1] ).toEqual( 'bar' );
        });

    });


});

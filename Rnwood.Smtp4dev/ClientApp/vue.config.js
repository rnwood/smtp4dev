module.exports = {
    devServer: {
        proxy: {
            '^/api' : {
                target: 'http://localhost:5000',
                changeOrigin:true
            },
            '^/hubs': {
                target: 'http://localhost:5000',
                changeOrigin:true
            }
        }
    },
    outputDir: '../wwwroot',
    publicPath: './',
    configureWebpack: {
        devtool: 'source-map'
    }, 
        productionSourceMap: false

}

const { defineConfig } = require('@vue/cli-service')
module.exports = defineConfig({
    transpileDependencies: true,
    productionSourceMap: false,
    devServer: {
        proxy: {
            '^/api': {
                target: 'http://localhost:5000',
                changeOrigin: true
            },
            '^/hubs': {
                target: 'http://localhost:5000',
                changeOrigin: true
            }
        }
    },
    outputDir: '../wwwroot',
    publicPath: './',
})

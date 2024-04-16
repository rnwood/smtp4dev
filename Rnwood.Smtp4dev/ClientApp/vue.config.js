const { defineConfig } = require('@vue/cli-service')
const NodePolyfillPlugin = require('node-polyfill-webpack-plugin')
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
    chainWebpack: config => {
        config.plugin('polyfills').use(NodePolyfillPlugin)
    },
    configureWebpack: {
        resolve: {
            fallback: {
                fs: false
            }
        }
    }
})

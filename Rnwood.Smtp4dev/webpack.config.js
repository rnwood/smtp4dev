var path = require('path');

module.exports = {
    entry: {
        site: ["./main.ts"]
    },
    output: {
        filename: 'bundle.js',
        path: path.resolve(__dirname, 'wwwroot/dist/')
    },
    module: {
        rules: [
            {
                test: /\.tsx?$/,
                loader: 'ts-loader',
                exclude: /node_modules/,
            },
        ]
    },
    resolve: {
        extensions: [".tsx", ".ts", ".js"],
		alias: {
			vue$: 'vue/dist/vue.esm.js'
		}
    }
};
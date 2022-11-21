const path = require("path");
const TerserPlugin = require("terser-webpack-plugin");

module.exports = {
    entry: {
        "match-list": path.resolve(__dirname, "Apps/match-list.jsx"),
        "message-list": path.resolve(__dirname, "Apps/message-list.jsx")
    },
    output: {
        filename: "[name].bundle.js",
        path: path.resolve(__dirname, "../Scripts/Dist")
    },
    resolve: {
        extensions: [".js", ".jsx"]
    },
    module: {
        rules: [
            {
                test: /\.(js|jsx)$/,
                exclude: /node_modules/,
                use: {
                    loader: "babel-loader"
                }
            },
            {
                test: /\.svg$/,
                use: {
                    loader: 'svg-url-loader',
                    options: {
                      limit: 10000,
                    },
                },
            }
        ]
    },
    optimization: {
        minimizer: [
            new TerserPlugin({ extractComments: false, terserOptions: { output: { comments: false } } })
        ],
    }
};

const path = require("path");
const TerserPlugin = require("terser-webpack-plugin");

module.exports = {
    entry: {
        "match-list": path.resolve(__dirname, "Apps/match-list.jsx")
        // more entrypoints can be added here to create other bundles to put on other pages
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
            }
        ]
    },
    optimization: {
        minimizer: [
            new TerserPlugin({ extractComments: false, terserOptions: { output: { comments: false } } })
        ],
    }
};

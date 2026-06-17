const path = require('path');

module.exports = {
  stories: ['../stories/**/*.stories.@(js|jsx|ts|tsx)'],

  addons: [
    '@storybook/addon-docs',
    '@espoc/storybook-addon-mock'
  ],

  framework: {
    name: '@storybook/react-webpack5',
    options: {}
  },

  core: {
    builder: '@storybook/builder-webpack5'
  },

  features: {
    backgrounds: false,
    changeDetection: false,
    interactions: false,
    sidebarOnboardingChecklist: false
  },

  typescript: {
    reactDocgen: 'react-docgen-typescript',
    reactDocgenTypescriptOptions: {
      tsconfigPath: path.resolve(__dirname, '../tsconfig.json')
    }
  },

  webpackFinal: async (config) => {
    config.module.rules.push({
      test: /\.(ts|tsx|js|jsx)$/,
      include: [
        path.resolve(__dirname, '../Apps'),
        path.resolve(__dirname, '../Components'),
        path.resolve(__dirname, '../Modules'),
        path.resolve(__dirname, '../Pages'),
        path.resolve(__dirname, '../stories'),
      ],
      use: {
        loader: 'babel-loader'
      }
    });

    return config;
  }
};
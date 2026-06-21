import type { StorybookConfig } from '@storybook/react-vite';

const config: StorybookConfig = {
  "stories": [
    "../src/**/*.stories.@(js|jsx|mjs|ts|tsx)"
  ],
  "addons": [],
  "framework": "@storybook/react-vite",

  "features": {
    "backgrounds": false,
    "changeDetection": false,
    "interactions": false,
    "sidebarOnboardingChecklist": false
  },

};
export default config;
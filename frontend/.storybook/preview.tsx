import { ChakraProvider } from '@chakra-ui/react'
import type { Preview } from '@storybook/react-vite'
import { predictTheme } from '../src/theme.ts'
import { UserProvider } from '../src/providers/UserProvider'
import { BrowserRouter } from 'react-router';

const preview: Preview = {
  decorators: [
    (Story, { parameters }) => (
      <ChakraProvider value={predictTheme}>
        <UserProvider mockUser={parameters.user ?? null}>
          <BrowserRouter>
            <Story />
          </BrowserRouter>
        </UserProvider>
      </ChakraProvider>
    )
  ],
  parameters: {
    controls: {
      matchers: {
        color: /(background|color)$/i,
        date: /Date$/i,
      },
    },
  },
};

export default preview;
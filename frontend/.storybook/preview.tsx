import { ChakraProvider } from '@chakra-ui/react'
import type { Preview } from '@storybook/react-vite'
import { predictTheme } from '../src/theme.ts'
import { UserProvider, type User } from '../src/providers/UserProvider'
import { ColorModeProvider } from '../src/components/ui/color-mode'
import { BrowserRouter } from 'react-router';

const preview: Preview = {
  decorators: [
    // Mirrors main.tsx's provider nesting - anything a component pulls from context there
    // (color mode, user) must be available here too or its stories crash.
    (Story, { parameters }) => (
      <ChakraProvider value={predictTheme}>
        <ColorModeProvider>
          <UserProvider mockUser={(parameters.user as User | null | undefined) ?? null}>
            <BrowserRouter>
              <Story />
            </BrowserRouter>
          </UserProvider>
        </ColorModeProvider>
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
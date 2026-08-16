// Mirrors main.tsx's font imports - without these, headings/body text fall back to the browser's
// system UI font instead of Manrope/Inter, which renders visibly heavier/different from the real site.
import '@fontsource/manrope/500.css'
import '@fontsource/manrope/700.css'
import '@fontsource/manrope/800.css'
import '@fontsource/inter/400.css'
import '@fontsource/inter/500.css'
import '@fontsource/inter/600.css'
import '@fontsource/inter/700.css'
import { ChakraProvider } from '@chakra-ui/react'
import type { Preview } from '@storybook/react-vite'
import { predictTheme } from '../src/theme.ts'
import { UserProvider, type User } from '../src/providers/UserProvider'
import { CompetitionProvider } from '../src/providers/CompetitionProvider'
import { defaultCompetitionContextValue, type CompetitionContextType } from '../src/hooks/useCompetition'
import { ColorModeProvider } from '../src/components/ui/color-mode'
import { BrowserRouter } from 'react-router';

const preview: Preview = {
  decorators: [
    // Mirrors main.tsx's provider nesting - anything a component pulls from context there
    // (color mode, user, competition) must be available here too or its stories crash.
    (Story, { parameters }) => (
      <ChakraProvider value={predictTheme}>
        <ColorModeProvider>
          <UserProvider mockUser={(parameters.user as User | null | undefined) ?? null}>
            <CompetitionProvider mockValue={(parameters.competition as CompetitionContextType | undefined) ?? defaultCompetitionContextValue}>
              <BrowserRouter>
                <Story />
              </BrowserRouter>
            </CompetitionProvider>
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
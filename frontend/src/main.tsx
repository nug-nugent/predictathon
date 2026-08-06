import '@fontsource/manrope/500.css'
import '@fontsource/manrope/700.css'
import '@fontsource/manrope/800.css'
import '@fontsource/inter/400.css'
import '@fontsource/inter/500.css'
import '@fontsource/inter/600.css'
import '@fontsource/inter/700.css'
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router'
import { SiteRoutes } from './routes/Routes.tsx'
import { UserProvider } from './providers/UserProvider.tsx'
import { CompetitionProvider } from './providers/CompetitionProvider.tsx'
import { ChakraProvider } from '@chakra-ui/react'
import { predictTheme } from './theme.ts'
import { ColorModeProvider } from './components/ui/color-mode.tsx'
import { ErrorBoundary } from './components/ui/error-boundary.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ChakraProvider value={predictTheme}>
      <ColorModeProvider>
        <ErrorBoundary>
          <UserProvider>
            <CompetitionProvider>
              <BrowserRouter>
                <SiteRoutes />
              </BrowserRouter>
            </CompetitionProvider>
          </UserProvider>
        </ErrorBoundary>
      </ColorModeProvider>
    </ChakraProvider>
  </StrictMode>
)

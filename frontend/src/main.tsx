import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router'
import { SiteRoutes } from './routes/Routes.tsx'
import { UserProvider } from './providers/UserProvider.tsx'
import { CompetitionProvider } from './providers/CompetitionProvider.tsx'
import { ChakraProvider } from '@chakra-ui/react'
import { predictTheme } from './theme.ts'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ChakraProvider value={predictTheme}>
      <UserProvider>
        <CompetitionProvider>
          <BrowserRouter>
            <SiteRoutes />
          </BrowserRouter>
        </CompetitionProvider>
      </UserProvider>
    </ChakraProvider>
  </StrictMode>
)

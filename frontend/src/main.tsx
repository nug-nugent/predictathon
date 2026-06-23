import { ChakraProvider } from "@chakra-ui/react";
import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { RouterProvider } from "react-router";
import { UserProvider } from "./providers/UserProvider.tsx";
import { router } from "./routes/Routes.tsx";
import { predictTheme } from "./theme.ts";

createRoot(document.getElementById("root")!).render(
    <StrictMode>
        <ChakraProvider value={predictTheme}>
            <UserProvider>
                <RouterProvider router={router} />
            </UserProvider>
        </ChakraProvider>
    </StrictMode>
);

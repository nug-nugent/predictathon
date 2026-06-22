import { createSystem, defaultConfig } from "@chakra-ui/react";

export const predictTheme = createSystem(defaultConfig, {
    theme: {
        tokens: {
            // fonts: {
            //   heading: { value: `'Open Sans', sans-serif` },
            //   body: { value: `'Open Sans', sans-serif` },
            // },
        },
        semanticTokens: {
            colors: {
                points: {
                    0: { value: "#EE0000" },
                    1: { value: "#FF6600" },
                    2: { value: "#888800" },
                    3: { value: "#5cb85c" }
                }
            }
        }
    }
});

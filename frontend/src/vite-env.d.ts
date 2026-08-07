/// <reference types="vite/client" />

interface ImportMetaEnv {
    readonly VITE_API_BASE_URL: string;
    readonly VITE_PAYPAL_CLIENT_ID: string;
}

interface ImportMeta {
    readonly env: ImportMetaEnv;
}

// Injected at build time by vite.config.ts's `define`, from frontend/package.json's version.
declare const __APP_VERSION__: string;

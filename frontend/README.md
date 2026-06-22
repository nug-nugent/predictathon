# Predictathon UI

This is the Predictathon frontend app.

Install dependencies: `npm install`

Components demo: `npm run storybook` - http://localhost:6006

Run locally: `npm run dev` - http://localhost:5173

Production build: `npm run build` - outputs to `./dist/`

## Developing in VS Code

1. Install the "Prettier - Code formatter" extension.
2. Create or update `.vscode/settings.json` as follows:

```json
{
    "editor.codeActionsOnSave": {
        "source.organizeImports": "explicit"
    },
    "editor.defaultFormatter": "esbenp.prettier-vscode",
    "editor.formatOnSave": true,
    "editor.formatOnPaste": true
}
```

These settings will run Prettier formatting when saving files. To run it manually use `npm run pretty`.

## Tools used

- React: Application framework
- Typescript: Language
- Vite: Build tools
- react-router: Page routing/protection
- Chakra: UI component library - https://chakra-ui.com/docs/components/concepts/overview
- Lucide: Icon library - https://lucide.dev/icons/
- Storybook: Component demos

Possible future additions:

- GIF picker that supports KLIPY: https://dokurno.dev/gif-picker-react/
- Emjoi picker: https://ealush.com/emoji-picker-react/#playground
- (there may be alternatives to both)

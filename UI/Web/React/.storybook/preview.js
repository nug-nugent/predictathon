import '../../Styles/Predictathon.css';

const viewports = {
  iphone12: {
    name: 'iPhone 12',
    styles: {
      width: '390px',
      height: '844px',
    },
    type: 'mobile',
  },
  pixel5: {
    name: 'Pixel 5',
    styles: {
      width: '393px',
      height: '851px',
    },
    type: 'mobile',
  },
  ipadAir: {
    name: 'iPad Air',
    styles: {
      width: '820px',
      height: '1180px',
    },
    type: 'tablet',
  },
}

export const parameters = {
  controls: {
    matchers: {
      color: /(background|color)$/i,
      date: /Date$/,
    },
  },
  viewport: { options: viewports },
  backgrounds: { grid: { disable: true }, disabled: true },
  options: {
    storySort: {
      order: [ "Match List", ["Match List Page"] ]
    }
  }
}
export const tags = ['autodocs'];
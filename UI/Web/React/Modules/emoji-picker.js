import { EmojiButton } from '@joeattardi/emoji-button';

export class EmojiPicker {
    constructor(onEmojiSelected, customReactionsPath) {
        this.popup = new EmojiButton({
            style: 'twemoji',
            initialCategory: 'recents',
            zIndex: 2500, // nav bar is 2000
            showAnimation: false,
            i18n: { // needed only to change the 'custom' category name
                categories: {
                    recents: 'Recent',
                    smileys: 'Smileys',
                    people: 'People',
                    animals: 'Animals & Nature',
                    food: 'Food & Drink',
                    activities: 'Activities',
                    travel: 'Travel & Places',
                    objects: 'Objects',
                    symbols: 'Symbols',
                    flags: 'Flags',
                    custom: 'Predictathon'
                }
            },
            custom: getCustomEmojis(customReactionsPath)
        });

        this.popup.on("emoji", (e) => onEmojiSelected && onEmojiSelected(this.externalId, e.name, e.url));
    }

    async toggle(externalId, element) {
        this.externalId = externalId;
        this.popup.togglePicker(element);
    }
}

const getCustomEmojis = (customReactionsPath) => [
    {
        name: 'brewdog',
        emoji: customReactionsPath + '/brewdog.png'
    },
    {
        name: 'guinness',
        emoji: customReactionsPath + '/guinness.png'
    },
    {
        name: 'ludo',
        emoji: customReactionsPath + '/ludo.png'
    },
    {
        name: 'red card',
        emoji: customReactionsPath + '/red-card.png'
    },
    {
        name: 'yellow card',
        emoji: customReactionsPath + '/yellow-card.png'
    },
    {
        name: 'pussy time',
        emoji: customReactionsPath + '/pt.png'
    }
];

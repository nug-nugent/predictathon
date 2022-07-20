// wrapper to import and configure emoji-button.
// this should be referenced in a <script type="module"> tag.

import { EmojiButton } from 'https://cdn.jsdelivr.net/npm/@joeattardi/emoji-button@4.6.0';

window.initEmojiButton = function(imagesPath) {

	window.emojiButton = new EmojiButton({
		style: 'twemoji', // image-based with the most emoji support
		initialCategory: 'recents', // start at the top
		zIndex: 2500, // nav bar is 2000
		showAnimation: false, // loads faster without it
		i18n: { // needed if only to change the 'custom' category name
			search: 'Search...',
			notFound: 'Found fuck all',
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
		custom: [  // make sure the 'name' is unique and not already in use by a standard emoji
			{
				name: 'brewdog',
				emoji: imagesPath + '/brewdog.png'
			},
			{
				name: 'guinness',
				emoji: imagesPath + '/guinness.png'
			},
			{
				name: 'ludo',
				emoji: imagesPath + '/ludo.png'
			},
			{
				name: 'red card',
				emoji: imagesPath + '/red-card.png'
			},
			{
				name: 'yellow card',
				emoji: imagesPath + '/yellow-card.png'
			},
			{
				name: 'pussy time',
				emoji: imagesPath + '/pt.png'
			}
		]
	});
}
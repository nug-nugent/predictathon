import { getReactionImageUrl } from "../services/api";

// The 12 legacy custom Predictathon reactions (ported from the old emoji-picker.js), plus the
// three UK subdivision flags - included here as well as being standard (tag-sequence) Unicode
// emoji, since not every emoji-mart data set surfaces them as searchable/pickable on their own.
// All images are self-hosted (WebApi/Assets/Reactions) - see that folder's NOTICE.md - not any
// third-party CDN, so historical and newly-added reactions share one stable URL scheme.
const CUSTOM_REACTIONS: { id: string; name: string; filename: string }[] = [
    { id: "brewdog", name: "brewdog", filename: "brewdog.png" },
    { id: "guinness", name: "guinness", filename: "guinness.png" },
    { id: "ludo", name: "ludo", filename: "ludo.png" },
    { id: "red_card", name: "red card", filename: "red-card.png" },
    { id: "yellow_card", name: "yellow card", filename: "yellow-card.png" },
    { id: "pussy_time", name: "pussy time", filename: "pt.png" },
    { id: "beaker", name: "beaker", filename: "beaker.png" },
    { id: "facepalm", name: "facepalm", filename: "facepalm.png" },
    { id: "rick_roll", name: "rick roll", filename: "rick.png" },
    { id: "success_boy", name: "success boy", filename: "success.png" },
    { id: "vault_boy", name: "vault boy", filename: "vaultboy.jpg" },
    { id: "woo_hoo", name: "woo hoo", filename: "woo.png" },
    { id: "flag_england", name: "flag: England", filename: "1f3f4-e0067-e0062-e0065-e006e-e0067-e007f.svg" },
    { id: "flag_scotland", name: "flag: Scotland", filename: "1f3f4-e0067-e0062-e0073-e0063-e0074-e007f.svg" },
    { id: "flag_wales", name: "flag: Wales", filename: "1f3f4-e0067-e0062-e0077-e006c-e0073-e007f.svg" },
];

// emoji-mart's `custom` prop shape: an array of categories, each with an `emojis` array.
export const CUSTOM_REACTION_CATEGORY = [
    {
        id: "predictathon",
        name: "Predictathon",
        emojis: CUSTOM_REACTIONS.map((r) => ({
            id: r.id,
            name: r.name,
            keywords: [r.name],
            skins: [{ src: getReactionImageUrl(r.filename) }],
        })),
    },
];

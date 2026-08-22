import { getReactionImageUrl } from "../services/api";
import { getReactionCatalogue, type CustomReaction } from "../services/messageboard-service";

// Predictathon's own custom reactions (the 12 ported from the legacy emoji-picker.js) now live in
// a server-side manifest next to the image files - WebApi/Assets/Reactions/custom-reactions.json,
// see that folder's NOTICE.md - rather than being hardcoded here. That keeps one source of truth
// for what a reaction identity resolves to, and means adding a new one is a file drop plus a
// manifest line, with no frontend rebuild.
//
// The three UK subdivision flags used to be listed here as well. They aren't any more: the
// emoji-mart dataset the picker already ships surfaces them as standard emoji (id "flag-england"
// etc), so duplicating them produced two pickable entries for one image, under two different
// names - and since reactions group by identity, the same flag could show as two separate pills
// on one message.

// Fetched once per page load and shared - the manifest is static content that can't change while
// the app is running.
let cataloguePromise: Promise<CustomReaction[]> | null = null;

function fetchCatalogue(): Promise<CustomReaction[]> {
    cataloguePromise ??= getReactionCatalogue();
    return cataloguePromise;
}

// emoji-mart's `custom` prop shape: an array of categories, each with an `emojis` array. The `id`
// carries our namespaced reaction identity straight through, so onEmojiSelect gets back something
// the server can resolve without the client having to know any filenames.
export async function getCustomReactionCategory() {
    let customReactions: CustomReaction[];
    try {
        customReactions = await fetchCatalogue();
    } catch (error) {
        // A picker missing its custom category is a much better failure than no picker at all -
        // every standard emoji still works, since that dataset is bundled, not fetched.
        console.error("Failed to load the custom reaction catalogue", error);
        cataloguePromise = null;
        return [];
    }

    return [
        {
            id: "predictathon",
            name: "Predictathon",
            emojis: customReactions.map((r) => ({
                id: `c:${r.id}`,
                name: r.name,
                keywords: [r.name],
                skins: [{ src: getReactionImageUrl(r.imageFile) }],
            })),
        },
    ];
}

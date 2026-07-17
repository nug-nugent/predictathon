import brewdog from "../assets/reactions/brewdog.png";
import guinness from "../assets/reactions/guinness.png";
import ludo from "../assets/reactions/ludo.png";
import redCard from "../assets/reactions/red-card.png";
import yellowCard from "../assets/reactions/yellow-card.png";
import pussyTime from "../assets/reactions/pt.png";
import beaker from "../assets/reactions/beaker.png";
import facepalm from "../assets/reactions/facepalm.png";
import rickRoll from "../assets/reactions/rick.png";
import successBoy from "../assets/reactions/success.png";
import vaultBoy from "../assets/reactions/vaultboy.jpg";
import wooHoo from "../assets/reactions/woo.png";

export type MessageReactionOption = { name: string; imageUrl: string };

// Ported from the legacy UI/Web/React/Modules/emoji-picker.js custom reaction list - keep in sync
// with that file if the catalog ever changes there. Order matches the legacy picker.
export const MESSAGE_REACTIONS: MessageReactionOption[] = [
    { name: "brewdog", imageUrl: brewdog },
    { name: "guinness", imageUrl: guinness },
    { name: "ludo", imageUrl: ludo },
    { name: "red card", imageUrl: redCard },
    { name: "yellow card", imageUrl: yellowCard },
    { name: "pussy time", imageUrl: pussyTime },
    { name: "beaker", imageUrl: beaker },
    { name: "facepalm", imageUrl: facepalm },
    { name: "rick roll", imageUrl: rickRoll },
    { name: "success boy", imageUrl: successBoy },
    { name: "vault boy", imageUrl: vaultBoy },
    { name: "woo hoo", imageUrl: wooHoo },
];

/**
 * Cell padding for data tables, trimmed below `md`.
 *
 * Several of the tables here are only 20-40px wider than a phone at Chakra's default `sm` cell
 * padding, and that difference is the whole distance between a table that fits and one that has to
 * be scrolled sideways inside its own card. Shared rather than repeated per table so the tables
 * that need it stay in step, and so there is one place to change it.
 *
 * Pass to a `Table.Root` as `css={compactCellsOnSmallScreens}`.
 */
export const compactCellsOnSmallScreens = {
    "& th, & td": { paddingInline: { base: "6px", md: "8px" } },
};

/**
 * Lets an unbreakable single-word value - a username, an email address - break mid-word rather than
 * set the minimum width of its whole column. Without it the longest name on the page decides how
 * wide the table has to be, which is routinely more than a phone has.
 *
 * Reaches descendants as well as the cell itself: `overflow-wrap` is inherited, but a link inside
 * the cell brings its own, and the name is usually a link.
 *
 * Pass to a `Table.Cell` as `css={breakableCellText}`.
 */
export const breakableCellText = {
    "&, & *": { overflowWrap: "anywhere" },
};

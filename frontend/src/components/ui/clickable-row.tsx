import { Table } from "@chakra-ui/react";

// A Table.Row that acts as a click target the keyboard can reach too: focusable via Tab, and
// activated with Enter or Space like a real control - a bare onClick on a row is invisible to
// keyboard and assistive-tech users.
export function ClickableRow({ onActivate, children, ...rest }: Table.RowProps & { onActivate: () => void }) {
    return (
        <Table.Row
            tabIndex={0}
            cursor="pointer"
            onClick={onActivate}
            onKeyDown={(e) => {
                if (e.key === "Enter" || e.key === " ") {
                    // preventDefault stops Space from also scrolling the page.
                    e.preventDefault();
                    onActivate();
                }
            }}
            _hover={{ bg: "bg.muted" }}
            _focusVisible={{ bg: "bg.muted", outline: "2px solid", outlineColor: "input.borderFocus", outlineOffset: "-2px" }}
            {...rest}
        >
            {children}
        </Table.Row>
    );
}

import { Box, Flex, HStack, IconButton, NativeSelect, Text, useMediaQuery } from "@chakra-ui/react";
import { ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight } from "lucide-react";

type WeekPickerProps = {
    weeks: string[];
    selectedWeek: string;
    onWeekChange: (dateFrom: string) => void;
    /** The weeks that still have matches left to predict. */
    outstanding?: Set<string>;
};

function formatWeek(week: string, hasOutstanding: boolean, longMonth: boolean): string {
    // Browser locale (not a hardcoded one) - matches every other date in the app.
    const formatted = new Date(week).toLocaleDateString(undefined, { month: longMonth ? "long" : "short", day: "numeric", year: "numeric" });

    // A native <option> can't carry a badge element, so the marker has to be part of its text. A
    // bare star keeps it to a few pixels - the count lives in a real, styleable element alongside.
    return hasOutstanding ? `${formatted} *` : formatted;
}

// Clamps the day-of-month to the last day of the target month (matches date-fns' addMonths), so
// e.g. 31 Jan + 1 month lands on 28/29 Feb rather than overflowing into March.
function addMonths(date: Date, months: number): Date {
    const day = date.getDate();
    const result = new Date(date.getFullYear(), date.getMonth() + months, 1);
    const daysInResultMonth = new Date(result.getFullYear(), result.getMonth() + 1, 0).getDate();
    result.setDate(Math.min(day, daysInResultMonth));
    return result;
}

export function WeekPicker({ weeks, selectedWeek, onWeekChange, outstanding }: WeekPickerProps) {
    // A native <select> is as wide as its longest option, so a spelled-out "September 11, 2026"
    // alongside four nav buttons is wider than a phone - which used to push the month buttons onto
    // a second row. Below md the month is abbreviated and the Month/Week captions stay hidden,
    // which keeps the picker on one line. ssr:false so the very first render already knows which
    // of the two it is, rather than rendering the long form and then swapping it.
    const [isWideScreen] = useMediaQuery(["(min-width: 48em)"], { ssr: false });

    const weekDates = weeks.map((w) => new Date(w));
    const index = weeks.indexOf(selectedWeek);

    const prevWeekEnabled = index > 0;
    const nextWeekEnabled = index >= 0 && index < weeks.length - 1;

    // Month nav: jump to the nearest week at least a month earlier/later, ported from the legacy
    // WeekPicker's prevMonth/nextMonth logic.
    const findPrevMonthWeek = (): string | null => {
        if (index <= 0) return null;
        const monthEarlier = addMonths(weekDates[index], -1);
        for (let i = index - 1; i >= 0; i--) {
            if (monthEarlier >= weekDates[i]) return weeks[i];
        }
        return null;
    };

    const findNextMonthWeek = (): string | null => {
        if (index === -1 || index >= weeks.length - 1) return null;
        const monthLater = addMonths(weekDates[index], 1);
        for (let i = index; i < weeks.length - 1; i++) {
            if (monthLater < weekDates[i + 1]) return weeks[i];
        }
        return weeks[weeks.length - 1];
    };

    const prevMonthWeek = findPrevMonthWeek();
    const nextMonthWeek = findNextMonthWeek();

    return (
        <Flex justify="center" align="center" gap={{ base: 0, md: 4 }}>
            {/* No wrapping anywhere in this row: the abbreviated dates above keep it inside a phone
                already, and a stray nav button on its own second line is worse than a date that
                has to give up a few pixels to the ellipsis on the very narrowest screens. */}
            <HStack justify="center" gap={{ base: 1, md: 3 }} py={2} minW={0}>
                <NavButton enabled={!!prevMonthWeek} label="Month" onClick={() => prevMonthWeek && onWeekChange(prevMonthWeek)}>
                    <ChevronsLeft size={16} />
                </NavButton>
                <NavButton enabled={prevWeekEnabled} label="Week" onClick={() => prevWeekEnabled && onWeekChange(weeks[index - 1])}>
                    <ChevronLeft size={16} />
                </NavButton>

                <NativeSelect.Root width="auto" minW={0} size="sm">
                    <NativeSelect.Field value={selectedWeek} onChange={(e) => onWeekChange(e.target.value)} textOverflow="ellipsis">
                        {weeks.map((w) => <option key={w} value={w}>{formatWeek(w, outstanding?.has(w) ?? false, isWideScreen)}</option>)}
                    </NativeSelect.Field>
                    <NativeSelect.Indicator />
                </NativeSelect.Root>

                <NavButton enabled={nextWeekEnabled} label="Week" onClick={() => nextWeekEnabled && onWeekChange(weeks[index + 1])} isRight>
                    <ChevronRight size={16} />
                </NavButton>
                <NavButton enabled={!!nextMonthWeek} label="Month" onClick={() => nextMonthWeek && onWeekChange(nextMonthWeek)} isRight>
                    <ChevronsRight size={16} />
                </NavButton>
            </HStack>

            {/* Invisible spacer matching MatchStatus's width (and MatchRow's gap before it) below,
                so this picker's centerpoint lines up with the score column rather than the full
                row width - MatchStatus's fixed-width column throws off a plain center otherwise. */}
            <Box aria-hidden width={{ base: "0", md: "140px" }} flexShrink={0} />
        </Flex>
    );
}

function NavButton({ enabled, label, onClick, isRight, children }: {
    enabled: boolean;
    label: string;
    onClick: () => void;
    isRight?: boolean;
    children: React.ReactNode;
}) {
    return (
        <HStack gap={1} flexShrink={0} opacity={enabled ? 1 : 0.3} cursor={enabled ? "pointer" : "default"}
            onClick={onClick} flexDirection={isRight ? "row-reverse" : "row"}>
            <IconButton size="xs" variant="ghost" disabled={!enabled} aria-label={label}>
                {children}
            </IconButton>
            <Text fontSize="xs" display={{ base: "none", md: "block" }}>{label}</Text>
        </HStack>
    );
}

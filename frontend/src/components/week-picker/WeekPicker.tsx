import { Button, Center, HStack, Text } from "@chakra-ui/react";
import { addMonths, addWeeks, format } from "date-fns";
import { ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { Select } from "../select/Select";

export function WeekPicker({
    weeks,
    initialWeek,
    onWeekChange
}: {
    weeks: string[];
    initialWeek: string;
    onWeekChange: (week: string) => void;
}) {
    const [selectedWeek, setSelectedWeek] = useState(initialWeek);
    const [selectedWeekIndex, setSelectedWeekIndex] = useState(-1);
    const [prevWeekEnabled, setPrevWeekEnabled] = useState(false);
    const [nextWeekEnabled, setNextWeekEnabled] = useState(false);
    const [prevMonthEnabled, setPrevMonthEnabled] = useState(false);
    const [nextMonthEnabled, setNextMonthEnabled] = useState(false);

    const weekDates = useMemo(() => weeks?.map((w) => new Date(w)), [weeks]);

    const selectItems = useMemo(() => weeks?.map((w) => ({ label: format(w, "MMMM do yyyy"), value: w })), [weeks]);

    useEffect(() => {
        setSelectedWeek(initialWeek);
    }, [initialWeek]);

    useEffect(() => {
        if (!weeks) return;

        if (selectedWeekIndex !== -1) {
            onWeekChange && onWeekChange(selectedWeek);
        }

        const newIndex = weeks.findIndex((w) => w === selectedWeek);
        setSelectedWeekIndex(newIndex);
        setPrevWeekEnabled(newIndex > 0);
        setNextWeekEnabled(newIndex < weeks.length - 1);

        const newDate = weekDates[newIndex];
        setPrevMonthEnabled(addMonths(newDate, -1) >= weekDates[0]);
        setNextMonthEnabled(addWeeks(addMonths(newDate, 1), -1) <= weekDates[weekDates.length - 1]);
    }, [selectedWeek]);

    const onSelectChange = (value: string | null) => {
        setSelectedWeek(value!);
    };

    const prevWeek = () => {
        setSelectedWeek(weeks[selectedWeekIndex - 1]);
    };

    const nextWeek = () => {
        setSelectedWeek(weeks[selectedWeekIndex + 1]);
    };

    const prevMonth = () => {
        let monthEarlier = addMonths(weekDates[selectedWeekIndex], -1);
        for (let i = selectedWeekIndex - 1; i >= 0; i--) {
            if (monthEarlier >= weekDates[i]) {
                setSelectedWeek(weeks[i]);
                break;
            }
        }
    };

    const nextMonth = () => {
        let monthLater = addMonths(weekDates[selectedWeekIndex], 1);
        for (let i = selectedWeekIndex; i < weeks.length - 1; i++) {
            if (monthLater < weekDates[i + 1]) {
                setSelectedWeek(weeks[i]);
                return;
            }
        }

        setSelectedWeek(weeks[weeks.length - 1]);
    };

    return (
        <Center>
            <HStack gap={{ base: 1, sm: 2 }} w={{ base: "full", sm: "unset" }}>
                <Button
                    size={{ base: "xs", sm: "sm" }}
                    variant={"outline"}
                    rounded={"full"}
                    disabled={!prevMonthEnabled}
                    onClick={prevMonth}>
                    <ChevronsLeft /> <Text display={{ base: "none", md: "block" }}>Month</Text>
                </Button>
                <Button size={"xs"} variant={"outline"} rounded={"full"} disabled={!prevWeekEnabled} onClick={prevWeek}>
                    <ChevronLeft /> <Text display={{ base: "none", md: "block" }}>Week</Text>
                </Button>

                <Select
                    size={{ base: "xs", sm: "sm" }}
                    width={{ base: "full", sm: "200px" }}
                    items={selectItems}
                    value={selectedWeek}
                    onValueChange={onSelectChange}
                />

                <Button
                    size={{ base: "xs", sm: "sm" }}
                    variant={"outline"}
                    rounded={"full"}
                    disabled={!nextWeekEnabled}
                    onClick={nextWeek}>
                    <Text display={{ base: "none", md: "block" }}>Week</Text> <ChevronRight />
                </Button>
                <Button
                    size={{ base: "xs", sm: "sm" }}
                    variant={"outline"}
                    rounded={"full"}
                    disabled={!nextMonthEnabled}
                    onClick={nextMonth}>
                    <Text display={{ base: "none", md: "block" }}>Month</Text> <ChevronsRight />
                </Button>
            </HStack>
        </Center>
    );
}

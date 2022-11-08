import React, { useEffect, useState } from "react";
import { addMonths, format } from "date-fns";
import { Icon, icons } from "../../Modules/icons";
import { Container, ChevronContainer, SelectContainer } from "./styles";
import { addWeeks } from "date-fns/esm";
import { Select } from "../Select/Select";

export const WeekPicker = ({ weeks, initialWeek, onWeekChange }) => {
    const [selectedWeek, setSelectedWeek] = useState(initialWeek);
    const [selectedWeekIndex, setSelectedWeekIndex] = useState(-1);
    const [prevWeekEnabled, setPrevWeekEnabled] = useState(false);
    const [nextWeekEnabled, setNextWeekEnabled] = useState(false);
    const [prevMonthEnabled, setPrevMonthEnabled] = useState(false);
    const [nextMonthEnabled, setNextMonthEnabled] = useState(false);

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

    const weekDates = weeks?.map((w) => new Date(w));

    const onSelectChange = (e) => {
        setSelectedWeek(e.target.value);
    }

    const prevWeek = () => {
        setSelectedWeek(weeks[selectedWeekIndex - 1]);
    }

    const nextWeek = () => {
        setSelectedWeek(weeks[selectedWeekIndex + 1]);
    }

    const prevMonth = () => {
        let monthEarlier = addMonths(weekDates[selectedWeekIndex], -1);
        for (let i = selectedWeekIndex - 1; i >= 0; i--) {
            if (monthEarlier >= weekDates[i]) {
                setSelectedWeek(weeks[i]);
                break;
            }
        }
    }

    const nextMonth = () => {
        let monthLater = addMonths(weekDates[selectedWeekIndex], 1);
        for (let i = selectedWeekIndex; i < weeks.length - 1; i++) {
            if (monthLater < weekDates[i + 1]) {
                setSelectedWeek(weeks[i]);
                return;
            }
        }

        setSelectedWeek(weeks[weeks.length - 1]);
    }

    return (
        <>
            {weeks && (
                <Container>
                    <ChevronContainer enabled={prevMonthEnabled} onClick={() => prevMonthEnabled && prevMonth()}>
                        <Icon icon={icons.chevronDoubleLeft} />
                        <span>Month</span>
                    </ChevronContainer>
                    <ChevronContainer enabled={prevWeekEnabled} onClick={() => prevWeekEnabled && prevWeek()}>
                        <Icon icon={icons.chevronLeft} />
                        <span>Week</span>
                    </ChevronContainer>

                    <SelectContainer>
                        <Select value={selectedWeek} onChange={onSelectChange}>
                            {weeks?.map((w, i) => <option key={i} value={w}>{format(weekDates[i], "MMMM do yyyy")}</option> )}                
                        </Select>
                    </SelectContainer>

                    <ChevronContainer isRight enabled={nextWeekEnabled} onClick={() => nextWeekEnabled && nextWeek()}>
                        <span>Week</span>
                        <Icon icon={icons.chevronRight} />
                    </ChevronContainer>
                    <ChevronContainer isRight enabled={nextMonthEnabled} onClick={() => nextMonthEnabled && nextMonth()}>
                        <span>Month</span>
                        <Icon icon={icons.chevronDoubleRight} />
                    </ChevronContainer>
                </Container>
            )}
        </>
    );
}

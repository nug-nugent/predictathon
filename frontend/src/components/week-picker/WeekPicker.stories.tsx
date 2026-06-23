import type { Meta, StoryObj } from "@storybook/react-vite";
import { fn } from "storybook/test";
import { WeekPicker as Component } from "./WeekPicker";

const meta = {
    title: "Components/Week Picker",
    component: Component,
    args: {
        weeks: [
            "2022-08-12T00:00:00",
            "2022-08-19T00:00:00",
            "2022-08-26T00:00:00",
            "2022-09-02T00:00:00",
            "2022-09-09T00:00:00",
            "2022-09-16T00:00:00",
            "2022-09-23T00:00:00",
            "2022-09-30T00:00:00",
            "2022-10-07T00:00:00",
            "2022-10-14T00:00:00",
            "2022-10-21T00:00:00",
            "2022-10-28T00:00:00"
        ],
        initialWeek: "2022-08-12T00:00:00",
        onWeekChange: fn()
    }
} satisfies Meta<typeof Component>;

export default meta;
type Story = StoryObj<typeof meta>;

export const WeekPicker: Story = {};

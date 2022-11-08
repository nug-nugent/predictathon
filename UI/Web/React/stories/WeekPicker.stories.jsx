import React from 'react';
import { WeekPicker } from '../Components/WeekPicker/WeekPicker';

const weeks = [
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
    "2022-10-28T00:00:00",
];

export default {
    title: "Match List/Week Picker Component",
    component: WeekPicker,
    argTypes: {
        initialWeek: {
            description: "The initial value",
            options: weeks,
            control: "select"
        },
        onWeekChange: { description: "Called when the value is changed", action: "week changed"},
    }
}

const Template = (args) => <WeekPicker {...args} />;
const BaseArgs = {
    weeks: weeks,
    initialWeek: weeks[0]
};

export const Desktop = Template.bind({});
Desktop.args = BaseArgs;

export const Mobile = Template.bind({});
Mobile.args = BaseArgs;
Mobile.parameters = { viewport: { defaultViewport: 'pixel5' } };

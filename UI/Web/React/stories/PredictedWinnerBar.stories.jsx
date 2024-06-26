import React from 'react';
import { PredictedWinnerBar } from '../Components/Match/PredictedWinnerBar/PredictedWinnerBar';

export default {
    title: "Match List/PredictedWinnerBar Component",
    component: PredictedWinnerBar
};

const Template = (args) => <div style={{ width: args.width }}><PredictedWinnerBar {...args} /></div>;
const BaseArgs = {
    homeWin: 20,
    draw: 5,
    awayWin: 8,
    width: 230
};

export const Narrow = Template.bind({});
Narrow.args = BaseArgs;

export const Wider = Template.bind({});
Wider.args = {
    ...BaseArgs,
    width: 400
};


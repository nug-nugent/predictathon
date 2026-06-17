import { addMinutes, addHours, addDays, roundToNearestMinutes } from 'date-fns';
import React from 'react';
import { MatchList } from '../Pages/MatchList/MatchList';

const now = roundToNearestMinutes(new Date(), { roundingMethod: "ceil" });
const baseMatch = {
    predictedHomeGoals: null, 
    predictedAwayGoals: null, 
    actualHomeGoals: null, 
    actualAwayGoals: null, 
    points: null
};
const matches = [
    {
        ...baseMatch,
        id: "completed",
        description: "A complete match",
        homeTeam: "Netherlands",
        homeImage: "International/Europe/Netherlands.gif",
        awayTeam: "Czech Republic",
        awayImage: "International/Europe/CzechRepublic.gif",
        date: addDays(now, -1).toISOString(),
        predictedHomeGoals: 2,
        predictedAwayGoals: 1,
        actualHomeGoals: 1,
        actualAwayGoals: 0,
        points: 1
    },
    {
        ...baseMatch,
        id: "in-progress",
        description: "An in progress match",
        homeTeam: "West Bromwich Albion",
        homeImage: "WestBrom.gif",
        awayTeam: "Manchester United",
        awayImage: "ManUtd.gif",
        date: addMinutes(now, -10).toISOString(),
        predictedHomeGoals: 2,
        predictedAwayGoals: 1, 
    },
    {
        ...baseMatch,
        id: "almost-starting",
        description: "1-2 minutes left (not predicted)",
        homeTeam: "Costa Rica",
        homeImage: "International/CostaRica.gif",
        awayTeam: "England",
        awayImage: "International/Europe/England.gif",
        date: addMinutes(now, 6).toISOString(),
    },
    {
        ...baseMatch,
        id: "5-hours-left",
        description: "5 hours left",
        homeTeam: "Arsenel",
        homeImage: "Arsenal.gif",
        awayTeam: "Aston Villa",
        awayImage: "AstonVilla.gif",
        date: addHours(now, 5).toISOString(),
        predictedHomeGoals: 2,
        predictedAwayGoals: 1
    },
    {
        ...baseMatch,
        id: "1-day-14-hours-left",
        description: "predict error",
        homeTeam: "Netherlands",
        homeImage: "International/Europe/Netherlands.gif",
        awayTeam: "Czech Republic",
        awayImage: "International/Europe/CzechRepublic.gif",
        date: addDays(addHours(now, 14), 1).toISOString(),
        isKnockout: true
    },
    {
        ...baseMatch,
        id: "1-day-14-hours-left-1",
        homeTeam: "West Bromwich Albion",
        homeImage: "WestBrom.gif",
        awayTeam: "Manchester United",
        awayImage: "ManUtd.gif",
        date: addDays(addHours(now, 14), 1).toISOString(),
        isKnockout: true
    },
    {
        ...baseMatch,
        id: "1-day-14-hours-left-2",
        homeTeam: "Costa Rica",
        homeImage: "International/CostaRica.gif",
        awayTeam: "England",
        awayImage: "International/Europe/England.gif",
        date: addDays(addHours(now, 14), 1).toISOString(),
        isKnockout: true
    },
    {
        ...baseMatch,
        id: "1-day-14-hours-left-3",
        homeTeam: "Arsenel",
        homeImage: "Arsenal.gif",
        awayTeam: "Aston Villa",
        awayImage: "AstonVilla.gif",
        date: addDays(addHours(now, 14), 1).toISOString(),
        isKnockout: true
    },
]


export default {
    title: "Match List/Match List Page",
    component: MatchList,
    parameters: {
        layout: "fullscreen",
        docs: { description: { component: "The **MatchList** components renders the **Match** components for the provided list of matches, grouped by date."
            + "\n\nIt is also responsible for updating their realtime props **matchStatus** and **minutesToPredict** at the start of every minute"
            + " and making the relevant API calls to save predictions and retrieve the predictions/results list."
            + "\n\nThe example matches below all use their description prop to describe how they are configured."
            + " All dates are relative to when the page was loaded." } },
        mockData: [
            {
                url: "Predictions.aspx?CallBack=SavePrediction",
                method: 'POST',
                status: 200,
                delay: 600,
                response: ({ body }) => body.get("MatchID") !== "1-day-14-hours-left",
            },
            {
                url: "Predictions.aspx?CallBack=GetPredictions&MatchID=:id",
                method: 'GET',
                status: 200,
                delay: 600,
                response: ({ searchParams }) => {
                    return searchParams.MatchID == "completed"
                        ? [
                            {username: "Diamond Geezer", homeGoals: 2, awayGoals: 0, points: 3},
                            {username: "mubba", homeGoals: 2, awayGoals: 0, points: 3},
                            {username: "swingitinthemixer", homeGoals: 2, awayGoals: 0, points: 3},
                            {username: "wiztipskimor", homeGoals: 2, awayGoals: 0, points: 3},
                            {username: "arunc101", homeGoals: 2, awayGoals: 1, points: 2},
                            {username: "bigpony", homeGoals: 2, awayGoals: 1, points: 2},
                            {username: "klasing", homeGoals: 2, awayGoals: 1, points: 2, isMe: true},
                            {username: "kurt_hustle", homeGoals: 2, awayGoals: 1, points: 2},
                            {username: "Wfcmoog", homeGoals: 2, awayGoals: 1, points: 2},
                            {username: "Fe", homeGoals: 1, awayGoals: 0, points: 1},
                            {username: "Suzan Ham", homeGoals: 1, awayGoals: 0, points: 1},
                            {username: "astronautis", homeGoals: 1, awayGoals: 1, points: 0},
                            {username: "BubbaGump", homeGoals: 2, awayGoals: 2, points: 0},
                            {username: "heva", homeGoals: 1, awayGoals: 1, points: 0},
                            {username: "JP", homeGoals: 1, awayGoals: 1, points: 0},
                            {username: "Nugsson", homeGoals: 1, awayGoals: 1, points: 0},
                            {username: "toni the poni", homeGoals: 1, awayGoals: 1, points: 0},
                            {username: "Scarlett13", homeGoals: 1, awayGoals: 2, points: 0},
                            {username: "Phoenixkeeper", homeGoals: null, awayGoals: null, points: null},
                            {username: "The Bear", homeGoals: null, awayGoals: null, points: null},
                        ]
                        : [
                            {username: "arunc101", homeGoals: 2, awayGoals: 1},
                            {username: "astronautis", homeGoals: 1, awayGoals: 1},
                            {username: "bigpony", homeGoals: 2, awayGoals: 1},
                            {username: "BubbaGump", homeGoals: 2, awayGoals: 2},
                            {username: "Diamond Geezer", homeGoals: 2, awayGoals: 0},
                            {username: "Fe", homeGoals: 1, awayGoals: 0},
                            {username: "heva", homeGoals: 1, awayGoals: 1},
                            {username: "JP", homeGoals: 1, awayGoals: 1},
                            {username: "klasing", homeGoals: 2, awayGoals: 1, isMe: true},
                            {username: "kurt_hustle", homeGoals: 2, awayGoals: 1},
                            {username: "mubba", homeGoals: 2, awayGoals: 0},
                            {username: "Nugsson", homeGoals: 1, awayGoals: 1},
                            {username: "Phoenixkeeper", homeGoals: null, awayGoals: null},
                            {username: "Scarlett13", homeGoals: 1, awayGoals: 2},
                            {username: "Suzan Ham", homeGoals: 1, awayGoals: 0},
                            {username: "swingitinthemixer", homeGoals: 2, awayGoals: 0},
                            {username: "The Bear", homeGoals: null, awayGoals: null},
                            {username: "toni the poni", homeGoals: 1, awayGoals: 1},
                            {username: "wiztipskimor", homeGoals: 2, awayGoals: 0},
                            {username: "Wfcmoog", homeGoals: 2, awayGoals: 1},
                        ];
                }
            },
        ],
    }
};

const Template = (args) => <MatchList {...args} />;
const BaseArgs = {
    matches: matches,
    imagesPath: "http://predictathon.co.uk/Images/",
};

export const ExamplePage = Template.bind({});
ExamplePage.args = BaseArgs;

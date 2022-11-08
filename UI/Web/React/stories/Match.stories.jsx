import React from 'react';
import { Match } from '../Components/Match/Match';
import { MatchStatus, PredictionsListStatus, PredictionStatus } from '../Modules/constants';

const goalsArgType = {
    options: [null, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9],
    control: {
        type: "inline-radio",
        labels: { "null": "Not set" }
    }
};
const preParameters = { controls: { exclude: ["id", "predictionsListStatus", "predictionsList", "actualHomeGoals", "actualAwayGoals", "points"] } };
const postParameters = { controls: { exclude: ["id", "predictionStatus", "minutesToPredict" ] } };
const duringParameters = { controls: { exclude: ["id", "predictionStatus", "minutesToPredict", "actualHomeGoals", "actualAwayGoals", "points" ] } };

export default {
    title: "Match List/Match Component",
    component: Match,
    argTypes: {
        showHeaders: { description: "Shows the column headers above the match" },
        lastInGroup: { description: "Shows bottom rounded corners on mobile" },
        date: { control: "date" },
        description: { description: "Shown next to the kick-off time, e.g. Group A" },
        isKnockout: { description: "Shows a red asterick after the team names" },
        matchStatus: {
            options: ["Pre", "During", "Post"],
            control: {
                type: "inline-radio", labels: { Pre: "Before Match", During: "During Match", Post: "Post Match" }
            }
        },
        minutesToPredict: { description: "Used for the status countdown" },
        predictionStatus: {
            description: "The two different 'Saving' variations are to show the right background colour on mobile if the match is in less than a day",
            options: ["NotPredicted", "Predicted", "SavingFromNotPredicted",
                "SavingFromPredicted", "Saved", "SaveError"],
            control: {
                type: "inline-radio",
                labels: {
                    NotPredicted: "Not Predicted", Predicted: "Predicted",
                    SavingFromNotPredicted: "Saving From Not Predicted",
                    SavingFromPredicted: "Saving From Predicted",
                    Saved: "Saved", SaveError: "Save Error"
                }
            }
        },
        predictionsListStatus: {
            options: ["Loading", "LoadingFailed", "Loaded", "Open"],
            control: {
                type: "inline-radio", labels: { Pre: "Before Match", During: "During Match", Post: "Post Match" }
            }
        },
        predictedHomeGoals: goalsArgType,
        predictedAwayGoals: goalsArgType,
        actualHomeGoals: goalsArgType,
        actualAwayGoals: goalsArgType,
        points: {
            ...goalsArgType,
            options: [null, 0, 1, 2, 3]
        },
        onFocus: { description: "Called when either input gets focus", action: "focused"},
        onPredictionChanged: { description: "Called when we want to save", action: "prediction changed" },
        togglePredictionsList: { description: "Called when we want to show/hide the predictions list", action: "prediction list toggled" },
    },
    parameters: { 
        layout: "fullscreen",
        docs: { description: { component: "This is the main UI component for a match.  It is controlled by its props and callbacks (apart from the pagination on the predictions table)." } }
    }
};

const Template = (args) => <Match {...args} />;
const BaseArgs = {
    id: "867c9248-89c8-4f1a-879c-eabaf5b07d3f",
    showHeaders: true,
    lastInGroup: true,
    date: Date.parse("2018-06-14T15:00:00.000Z"),
    description: "Group A",
    isKnockout: false,
    matchStatus: MatchStatus.Pre,
    minutesToPredict: (24 + 14) * 60,
    predictionStatus: PredictionStatus.NotPredicted,
    homeTeam: "Netherlands",
    homeImage: "http://predictathon.co.uk/Images/TeamCrests/International/Europe/Netherlands.gif",
    awayTeam: "Czech Republic",
    awayImage: "http://predictathon.co.uk/Images/TeamCrests/International/Europe/CzechRepublic.gif",
    predictedHomeGoals: null,
    predictedAwayGoals: null,
    actualHomeGoals: null,
    actualAwayGoals: null,
    points: null,
    predictionsListStatus: PredictionsListStatus.Loaded,
    predictionsList: [
        { username: 'stu', homeGoals: 2, awayGoals: 1 },
    ]
};

export const BeforeMatch = Template.bind({});
BeforeMatch.args = {
    ...BaseArgs
};
BeforeMatch.parameters = preParameters;

export const BeforeMatchWithWarning = Template.bind({});
BeforeMatchWithWarning.args = {
    ...BaseArgs,
    minutesToPredict: 100
};
BeforeMatchWithWarning.parameters = preParameters;

export const PredictionSaving = Template.bind({});
PredictionSaving.args = {
    ...BaseArgs,
    predictionStatus: PredictionStatus.SavingFromNotPredicted,
    predictedHomeGoals: 2,
    predictedAwayGoals: 1
};
PredictionSaving.parameters = preParameters;

export const SavingWithWarning = Template.bind({});
SavingWithWarning.args = {
    ...BaseArgs,
    minutesToPredict: 100,
    predictionStatus: PredictionStatus.SavingFromNotPredicted,
    predictedHomeGoals: 2,
    predictedAwayGoals: 1
};
SavingWithWarning.parameters = preParameters;

export const PredictionSaved = Template.bind({});
PredictionSaved.args = {
    ...BaseArgs,
    predictionStatus: PredictionStatus.Saved,
    predictedHomeGoals: 2,
    predictedAwayGoals: 1
};
PredictionSaved.parameters = preParameters;

export const SaveError = Template.bind({});
SaveError.args = {
    ...BaseArgs,
    predictionStatus: PredictionStatus.SaveError
};
SaveError.parameters = preParameters;

export const DuringMatch = Template.bind({});
DuringMatch.args = {
    ...BaseArgs,
    matchStatus: MatchStatus.During,
    predictedHomeGoals: 2,
    predictedAwayGoals: 1,
    predictionsList: [
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
    ]
};
DuringMatch.parameters = duringParameters;

export const PredictionsListLoading = Template.bind({});
PredictionsListLoading.args = {
    ...{...DuringMatch.args},
    predictionsListStatus: PredictionsListStatus.Loading
};
PredictionsListLoading.parameters = duringParameters;

export const PredictionsListLoadError = Template.bind({});
PredictionsListLoadError.args = {
    ...{...DuringMatch.args},
    predictionsListStatus: PredictionsListStatus.LoadingFailed
};
PredictionsListLoadError.parameters = duringParameters;

export const PredictionsListOpen = Template.bind({});
PredictionsListOpen.args = {
    ...{...DuringMatch.args},
    predictionsListStatus: PredictionsListStatus.Open
};
PredictionsListOpen.parameters = duringParameters;

export const PostMatch = Template.bind({});
PostMatch.args = {
    ...BaseArgs,
    matchStatus: MatchStatus.Post,
    predictedHomeGoals: 2,
    predictedAwayGoals: 1,
    actualHomeGoals: 2,
    actualAwayGoals: 1,
    points: 3,
    predictionsList: [
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
};
PostMatch.parameters = postParameters;

export const PostMatchResultsOpen = Template.bind({});
PostMatchResultsOpen.args = {
    ...{ ...PostMatch.args },
    predictionsListStatus: PredictionsListStatus.Open
};
PostMatchResultsOpen.parameters = postParameters;

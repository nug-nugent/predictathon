import React, { Fragment, useEffect, useRef } from "react";
import useState from 'react-usestateref';
import { addMinutes, differenceInMilliseconds, differenceInMinutes, startOfMinute } from "date-fns";
import { Match } from "../../Components/Match/Match";
import { MatchStatus, PredictionsListStatus, PredictionStatus } from "../../Modules/constants";
import { Container, DateHeader, KnockoutWarningContainer, TextContainer } from "./styles";
import { WeekPicker } from "../../Components/WeekPicker/WeekPicker";
import { Icon, icons } from "../../Modules/icons";
import { toDate, formatInTimeZone } from "date-fns-tz";

const PredictionSaveDisplayTime = 4000;

const getTeamImage = (imagesPath, teamImage) => {
    return imagesPath + (teamImage ? "TeamCrests/" + teamImage : "Common/spacer.gif");
};

const calculateTimeProps = (match) => {
    match.minutesToPredict = differenceInMinutes(match.date, addMinutes(new Date(), 5), { roundingMethod: "ceil" });
    match.matchStatus = match.minutesToPredict < 1 
        ? (match.actualHomeGoals === null ? MatchStatus.During : MatchStatus.Post)
        : MatchStatus.Pre;

    return match;
};

const mapMatches = (matches, imagesPath) => {
    let focusGiven = false;

    return matches.map((m) => {
        const match = calculateTimeProps({
            ...m,
            date: toDate(m.date, { timeZone: m.timeZone }),
            homeImage: getTeamImage(imagesPath, m.homeImage),
            awayImage: getTeamImage(imagesPath, m.awayImage),

            predictionStatus: m.predictedHomeGoals === null
                ? PredictionStatus.NotPredicted
                : PredictionStatus.Predicted,
            predictionSaveTimeout: null,
            predictionsList: null,
            predictionsListStatus: PredictionsListStatus.NotLoaded
        });

        match.hasFocus = !focusGiven && match.matchStatus === MatchStatus.Pre
            && match.predictionStatus === PredictionStatus.NotPredicted;
        if (match.hasFocus) focusGiven = true;

        return match;
    });
}

export const MatchList = ({ matches, weeks, loadedWeek, imagesPath }) => {
    const [matchList, setMatchList, matchListRef] = useState(() => mapMatches(matches, imagesPath));
    const [isLoading, setLoading] = useState(false);
    const [showKnockoutWarning, setShowKnockoutWarning] = useState(false);

    useEffect(() => {
        setShowKnockoutWarning(matchList.some((m) => m.isKnockout));
    }, [matchList]);

    // callbacks
    const onMatchFocus = (matchId) => {
        matchListRef.current.forEach((m, i) => m.hasFocus = m.id == matchId);
        setMatchList([...matchListRef.current]);
    }

    const onWeekChanged = async (dateFrom) => {
        setLoading(true);

        try {
            const response = await fetch(`Predictions.aspx?CallBack=GetMatches&DateFrom=${dateFrom}`);
            if (!response.ok) throw new Error();
            
            setMatchList(mapMatches(await response.json(), imagesPath));
        } catch (_) {
            setMatchList([]);
        }

        setLoading(false);
    }

    const onPredictionChanged = async (matchId, homeGoals, awayGoals, focusNextMatch) => {
        const matchIndex = matchListRef.current.findIndex((m) => m.id == matchId);
        if (matchIndex === -1) return;

        const match = matchListRef.current[matchIndex];
        match.predictionStatus = match.predictionStatus == PredictionStatus.NotPredicted
            ? PredictionStatus.SavingFromNotPredicted
            : PredictionStatus.SavingFromPredicted;
        setMatchList([...matchListRef.current]);

        try {
            const response = await fetch("Predictions.aspx?CallBack=SavePrediction", {
                method: "POST",
                body: new URLSearchParams({
                    MatchID: matchId,
                    HomeTeamGoals: homeGoals,
                    AwayTeamGoals: awayGoals
                })
            });
            if (!response.ok || (await response.text() !== "true")) throw new Error();

            match.predictionStatus = PredictionStatus.Saved;
            match.predictedHomeGoals = homeGoals;
            match.predictedAwayGoals = awayGoals;

            match.predictionSaveTimeout && clearTimeout(match.predictionSaveTimeout);
            match.predictionSaveTimeout = setTimeout(() => {
                match.predictionStatus = PredictionStatus.Predicted;
                setMatchList([...matchListRef.current]);
            }, PredictionSaveDisplayTime);

            if (focusNextMatch) {
                matchListRef.current.forEach((m, i) => m.hasFocus = i == matchIndex + 1);
                if (matchIndex + 1 === matchListRef.current.length) {
                    document.activeElement.blur();
                }
            }
        } catch (e) {
            console.log(e);
            match.predictionStatus = PredictionStatus.SaveError;
        }

        setMatchList([...matchListRef.current]);
    };

    const togglePredictionsList = async (matchId) => {
        const match = matchListRef.current.find((m) => m.id == matchId);
        if (!match) return;

        switch (match.predictionsListStatus) {
            case PredictionsListStatus.NotLoaded:
            case PredictionsListStatus.LoadingFailed:
                match.predictionsListStatus = PredictionsListStatus.Loading;
                setMatchList([...matchListRef.current]);
    
                try {
                    const response = await fetch(`Predictions.aspx?CallBack=GetPredictions&MatchID=${matchId}`);
                    if (!response.ok) throw new Error();
                    
                    match.predictionsList = await response.json();
                    match.predictionsListStatus = PredictionsListStatus.Open;
                } catch (e) {
                    match.predictionsListStatus = PredictionsListStatus.LoadingFailed;
                }
    
                break;
            
            case PredictionsListStatus.Loaded:
                match.predictionsListStatus = PredictionsListStatus.Open;
                break;

            case PredictionsListStatus.Open:
                match.predictionsListStatus = PredictionsListStatus.Loaded;
        }
        setMatchList([...matchListRef.current]);
    }

    // recalculate time props every minute on the minute 
    const timer = useRef(0);
    const startNextInterval = () => {
        const now = new Date(); // We'll assume the client's clock is accurate
        const nextDue = differenceInMilliseconds(addMinutes(startOfMinute(now), 1), now);

        timer.current = setTimeout(() => {
            setMatchList(matchListRef.current.map((match) => calculateTimeProps(match)));
            startNextInterval();
        }, nextDue);
    };
    useEffect(() => {
        startNextInterval();
        return () => timer.current && clearTimeout(timer.current);;
    }, []);

    // render
    let lastDate = null;
    let lastDateTime = null;
    return (
        <Container>
            <WeekPicker weeks={weeks} initialWeek={loadedWeek} onWeekChange={onWeekChanged} />

            {!matchList || matchList.length === 0 ? (
                <TextContainer>
                    {isLoading ? <Icon icon={icons.loading} pulse size="2x" /> : "No matches found."}
                </TextContainer>
            ) : (
                matchList.map((match, index) => {
                    let showDate = false;
                    let date = formatInTimeZone(match.date, match.timeZone, "EEEE do MMMM yyyy");
                    if (!lastDate || date !== lastDate) {
                        showDate = true;
                        lastDate = date;
                    }

                    let showHeaders = false;
                    if (!lastDateTime || match.date > lastDateTime) {
                        showHeaders = true;
                        lastDateTime = match.date;
                    }
                    
                    let lastInGroup = index === matchList.length - 1 || matchList[index + 1].date > match.date;

                    return (
                        <Fragment key={match.id}>
                            {showDate && <DateHeader>{date}</DateHeader>}
                            <Match {...match} showHeaders={showHeaders} lastInGroup={lastInGroup} togglePredictionsList={togglePredictionsList}
                                onFocus={onMatchFocus} onPredictionChanged={onPredictionChanged} />
                        </Fragment>
                    );
                })
            )}

            {showKnockoutWarning && (
                <KnockoutWarningContainer>
                    <span>* </span> Extra time excluded
                </KnockoutWarningContainer>
            )}
        </Container>
    );
}

import React, { useEffect, useRef } from "react";
import { addMinutes, formatDuration, intervalToDuration } from "date-fns";
import { Popover } from "react-tiny-popover";
import { Container, Result, Points, StatusIcon, statusStyles, PredictionsToggle, PredictionsContainer, StatusTextContainer, PredictionsIcon } from "./styles";
import { MatchStatus, PredictionsListStatus, PredictionStatus } from "../../../Modules/constants";
import { useIsInViewport } from "../../../Modules/use-is-in-viewport";
import { Table } from "../../Table/Table";
import { theme } from "../../../Modules/theme";
import { Icon } from "../../../Modules/icons";

const getStyle = (matchStatus, predictionStatus, minutesToPredict) => {
    if (matchStatus !== MatchStatus.Pre) return statusStyles.during;
    if (predictionStatus === PredictionStatus.SavingFromNotPredicted) {
        return minutesToPredict < 1440 ? statusStyles.savingFromWarning : statusStyles.saving;
    }
    if (predictionStatus === PredictionStatus.SavingFromPredicted) return statusStyles.saving;
    if (predictionStatus === PredictionStatus.Saved) return statusStyles.saved;
    if (predictionStatus === PredictionStatus.SaveError) return statusStyles.error;

    return predictionStatus === PredictionStatus.NotPredicted && minutesToPredict < 1440
        ? statusStyles.warning
        : statusStyles.pre;
};

const getText = (matchStatus, predictionStatus, date, minutesToPredict) => {
    if (matchStatus !== MatchStatus.Pre) return "Awaiting result";
    if (predictionStatus === PredictionStatus.Saved) return "Prediction saved!";
    if (predictionStatus === PredictionStatus.SaveError) return "Failed to save prediction!";

    const duration = intervalToDuration({ start: addMinutes(date, -minutesToPredict), end: date });
    const formats = duration.years
        ? ["years", "months"]
        : duration.months
            ? ["months", "days"]
            : duration.days
                ? ["days", "hours"]
                : duration.hours
                    ? ["hours", "minutes"]
                    : ["minutes"];

    return `${formatDuration(duration, { format: formats })} to predict`;
};

export const Status = ({ id, lastInGroup, matchStatus, predictionStatus, date,
        minutesToPredict, actualHomeGoals, actualAwayGoals, points, hasFocus,
        predictionsList, predictionsListStatus, togglePredictionsList }) => {
    const style = getStyle(matchStatus, predictionStatus, minutesToPredict);
    const text = getText(matchStatus, predictionStatus, date, minutesToPredict);

    // make sure this section is visible when we have focus (for mobile)
    const containerRef = useRef(null);
    const containerInView = useIsInViewport(containerRef, "100%");
    useEffect(() => {
        if (hasFocus && containerRef.current && !containerInView) {
            containerRef.current.scrollIntoView({ block: "end", inline: "nearest" });
        }
    }, [hasFocus]);

    const getArrow = () => {
        let icon = theme.predictionsListButtonExpand;
        let color = theme.predictionsListButtonColor;
        let pulse = false;
    
        switch (predictionsListStatus) {
            case PredictionsListStatus.Loading:
                icon = theme.predictionsListButtonLoading;
                pulse = true;
                break;
            case PredictionsListStatus.LoadingFailed:
                icon = theme.predictionsListButtonError;
                color = theme.predictionsListButtonErrorColor;
                break;
            case PredictionsListStatus.Open:
                icon = theme.predictionsListButtonCollapse;
        }
    
        return <Icon icon={icon} color={color} pulse={pulse} />;
    }

    return (
        <Container ref={containerRef} statusStyle={style} matchStatus={matchStatus} lastInGroup={lastInGroup}>
            {matchStatus === MatchStatus.Post ? (
                <>
                    <Result>
                        <div>Result:</div>
                        {` ${actualHomeGoals} - ${actualAwayGoals}`}
                    </Result>
                    <Points $points={points}>
                        <div>Points:</div>
                        {` ${points || 0}`}
                    </Points>
                </>
            ) : (
                <StatusTextContainer matchStatus={matchStatus}>
                    <StatusIcon icon={style.icon} color={style.iconColor} pulse={style.pulse} $matchStatus={matchStatus}
                        $mobileColor={style.mobileColor} $showOnMobile={style.iconOnmobile} />
                    {` ${text}`}
                </StatusTextContainer>
            )}

            {matchStatus !== MatchStatus.Pre && (
                <Popover
                    isOpen={predictionsListStatus === PredictionsListStatus.Open}
                    positions={["bottom", "top"]}
                    containerStyle={{ zIndex: 2500 }}
                    reposition="false"
                    align={"end"}
                    onClickOutside={() => togglePredictionsList(id)}
                    content={(
                        <PredictionsContainer>
                            <Table data={predictionsList} boldCheckFn={(p) => p.isMe}
                                columns={[
                                    { name: "username", minWidth: "150px" },
                                    {
                                        name: "prediction", align: "center",
                                        format: (p) => `${p.homeGoals === null ? "L" : p.homeGoals} - ${p.awayGoals === null ? "L" : p.awayGoals}`
                                    },
                                    ...matchStatus === MatchStatus.Post ? [{
                                        name: "points", align: "center", isGroup: true,
                                        format: (p) => p.points || 0
                                    }] : []
                                ]} />
                        </PredictionsContainer>
                    )}
                >
                    <PredictionsToggle title="View all predictions" onClick={() => togglePredictionsList(id)}>
                        <PredictionsIcon />
                        <span>All predictions </span>
                        {getArrow()}
                    </PredictionsToggle>
                </Popover>
            )}
        </Container>
    );
};

import React, { useEffect, useRef, useState } from "react";
import { MatchStatus } from "../../../Modules/constants";
import { Container, Team1, Team2, Flag1, Flag2, Input1, Input2, Dash } from './styles';

export const Details = ({ homeTeam, homeImage, awayTeam, awayImage, isKnockout, matchStatus, hasFocus,
    id, predictedHomeGoals, predictedAwayGoals, onFocus, onPredictionChanged }) => {
    const [homeInputValue, setHomeInputValue] = useState("");
    const [awayInputValue, setAwayInputValue] = useState("");
    const homeInput = useRef(null);
    const awayInput = useRef(null);

    useEffect(() => {
        hasFocus && document.activeElement !== awayInput.current && homeInput.current.focus();
    }, [hasFocus]);

    useEffect(() => {
        setHomeInputValue(predictedHomeGoals !== null ? predictedHomeGoals : "");
        setAwayInputValue(predictedAwayGoals !== null ? predictedAwayGoals : "");
    }, [predictedHomeGoals, predictedAwayGoals]);

    useEffect(() => {
        if (predictedHomeGoals === null) {
            if (matchStatus !== MatchStatus.Pre) {
                setHomeInputValue("L");
                setAwayInputValue("L");
            } else {
                setHomeInputValue("");
                setAwayInputValue("");
            }
        }
    }, [matchStatus]);

    const onInputFocus = (event) => {
        event.target.select();
        onFocus(id);
    };

    const onHomeInputChange = (event) => {
        const value = getValue(event.target.value);
        if (value === null) return;

        setHomeInputValue(value);
        save(value, awayInputValue, false);

        if (value !== "") {
            awayInput.current.focus();
        }
    }

    const onAwayInputChange = (event) => {
        const value = getValue(event.target.value);
        if (value === null) return;

        setAwayInputValue(value);
        save(homeInputValue, value, true);
    }

    const getValue = (value) => {
        return value === "" ? value : (value.match(/^[0-9]$/) ? parseInt(value) : null);
    };

    const save = (homeValue, awayValue, focusNextMatch) => {
        if (homeValue !== "" && awayValue !== "") {
            onPredictionChanged(id, homeValue, awayValue, focusNextMatch);
        }
    };

    return (
        <Container>
            <Team1>
                <div>{homeTeam}</div>
                <Flag1 src={homeImage} />
            </Team1>

            <Input1 ref={homeInput} matchStatus={matchStatus} value={homeInputValue}
                autoComplete="off" onFocus={onInputFocus} onChange={onHomeInputChange} />
            <Dash>-</Dash>
            <Input2 ref={awayInput} matchStatus={matchStatus} value={awayInputValue}
                autoComplete="off" onFocus={onInputFocus} onChange={onAwayInputChange} />

            <Team2 $isKnockout={isKnockout}>
                <Flag2 src={awayImage} />
                <div>{awayTeam}</div>
            </Team2>
        </Container>
    );
};

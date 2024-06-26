import React, { useEffect, useState } from "react";
import { Container, MostPredictedContainer } from "./styles";
import { PredictedWinnerBar } from "../PredictedWinnerBar/PredictedWinnerBar";

export const PredictionsSummary = ({ predictionsList }) => {
    const [data, setData] = useState();

    useEffect(() => {
        var homeWin = 0, draw = 0, awayWin = 0, list = [];

        const counts = predictionsList.reduce((output, item) => {
            if (item.homeGoals == null) return output;

            if (item.homeGoals > item.awayGoals) homeWin++;
            else if (item.homeGoals === item.awayGoals) draw++;
            else awayWin++;

            var scoreText = `${item.homeGoals} - ${item.awayGoals}`;
            output[scoreText] = (output[scoreText] || 0) + 1;

            if (output[scoreText] === 1) list.push(scoreText);

            return output;
        }, {});

        setData({
            topScores: list.sort((a, b) => counts[a] - counts[b]).reverse().slice(0, 3),
            counts,
            homeWin,
            draw,
            awayWin
        })
    }, [predictionsList]);

    return data?.topScores?.length > 0 &&
        <Container>
            <b>Predicted winner:</b>
            <PredictedWinnerBar homeWin={data.homeWin} draw={data.draw} awayWin={data.awayWin} />

            <MostPredictedContainer>
                <div><b>Top predictions:</b>&nbsp;</div>
                <div>
                    {data.topScores.map((score, i) =>
                        <span key={i}><b>{score}</b> ({data.counts[score]}){i+1 < data.topScores.length ? ", " : ""}</span>)}
                </div>
            </MostPredictedContainer>
       </Container>;
}
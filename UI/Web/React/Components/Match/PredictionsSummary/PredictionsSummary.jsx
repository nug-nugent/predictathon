import React, { useEffect, useState } from "react";
import { Container, Points, PointWinnersContainer, PredictedWinnerContainer, PredictedWinnerTitle } from "./styles";
import { PredictedWinnerBar } from "../PredictedWinnerBar/PredictedWinnerBar";
import { MatchStatus } from "../../../Modules/constants";
import { Table } from "../../Table/Table";

export const PredictionsSummary = ({ predictionsList, matchStatus }) => {
    const [data, setData] = useState();

    useEffect(() => {
        var homeWin = 0, draw = 0, awayWin = 0, scoresList = [],
            pointCounts = { 0:0, 1:0, 2:0, 3:0 };

        const scoreCounts = predictionsList.reduce((output, item) => {
            pointCounts[item.points || 0]++;

            if (item.homeGoals == null) return output;

            if (item.homeGoals > item.awayGoals) homeWin++;
            else if (item.homeGoals === item.awayGoals) draw++;
            else awayWin++;

            var scoreText = `${item.homeGoals} - ${item.awayGoals}`;
            output[scoreText] = (output[scoreText] || 0) + 1;

            if (output[scoreText] === 1) scoresList.push(scoreText);

            return output;
        }, {});

        setData({
            topScores: scoresList.sort((a, b) => scoreCounts[a] - scoreCounts[b]).reverse().slice(0, 4),
            scoreCounts,
            pointCounts: pointCounts,
            homeWin,
            draw,
            awayWin
        });
    }, [predictionsList]);

    return data?.topScores?.length > 0 &&
        <Container>
            <PredictedWinnerContainer>
                <PredictedWinnerTitle>PREDICTED WINNER</PredictedWinnerTitle>
                <PredictedWinnerBar homeWin={data.homeWin} draw={data.draw} awayWin={data.awayWin} />
            </PredictedWinnerContainer>

            <Table data={[{
                "firstCol": "# OF USERS",
                ...data.scoreCounts
            }]} columns={[
                { name: "firstCol", title: "TOP PREDICTIONS", width: "120px", mobileWidth: "90px", isHeaderColumn: true },
                ...data.topScores.map(score => ({ name: score, align: "center" }))
            ]} />

            {matchStatus === MatchStatus.Post && (
                <PointWinnersContainer>
                    <Table data={[{
                        "firstCol": "# OF USERS",
                        ...data.pointCounts
                    }]} columns={[
                        { name: "firstCol", title: "POINTS WON", width: "120px", mobileWidth: "90px", isHeaderColumn: true },
                        ...[3, 2, 1, 0].map(x => ({ name: x, title: <Points $points={x}>{x}</Points>, align: "center" }))
                    ]} />
                </PointWinnersContainer>
            )}
       </Container>;
}
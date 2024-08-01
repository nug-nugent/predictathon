import React, { useEffect, useState } from "react";
import { Bar, Bars, Container, TextBar } from "./styles";

export const PredictedWinnerBar = ({ homeWin, draw, awayWin }) => {
    const [data, setData] = useState();

    useEffect(() => {
        var total = homeWin + draw + awayWin;

        setData({
            homePercent: Math.round((100 * homeWin) / total),
            drawPercent: Math.round((100 * draw) / total),
            awayPercent: Math.round((100 * awayWin) / total)
        });
    }, [homeWin, draw, awayWin]);

    return data &&
        <Container>
            <Bars>
                <Bar percent={data.homePercent} isLeft={true} />
                <Bar percent={data.awayPercent} />
            </Bars>
            <TextBar>
                <div><b>{data.homePercent}%</b></div>
                <div>DRAW: <b>{data.drawPercent}%</b></div>
                <div><b>{data.awayPercent}%</b></div>
            </TextBar>
        </Container>;
}
import React from "react";
import { format } from "date-fns";
import { Container, KickOffAndTeam1, KickOff, Team1, Prediction, Team2, PostMatch, Result, Points, Status } from "./styles";
import { MatchStatus } from "../../../Modules/constants";

export const Header = ({ date, matchStatus }) => (
    <Container>
        <KickOffAndTeam1>
            <KickOff>Kick-off&nbsp;</KickOff>
            <div>{format(date, "HH:mm")}</div>
            <Team1>Home</Team1>
        </KickOffAndTeam1>
        <Prediction>Prediction</Prediction>
        <Team2>Away</Team2>
        {matchStatus === MatchStatus.Post ? (
            <PostMatch>
                <Result>Result</Result>
                <Points>Points</Points>
            </PostMatch>
        ) : <Status>Status</Status>}
    </Container>
);

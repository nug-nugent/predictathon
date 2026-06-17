import { formatInTimeZone } from "date-fns-tz";
import enGB from "date-fns/locale/en-GB";
import { Container, KickOffAndTeam1, KickOff, Team1, Prediction, Team2, PostMatch, Result, Points, Status } from "./styles";
import { MatchStatus } from "../../../Modules/constants";

interface HeaderProps {
    date: string;
    timeZone: string;
    matchStatus: MatchStatus;
}

export const Header = ({ date, timeZone, matchStatus }: HeaderProps) => (
    <Container>
        <KickOffAndTeam1>
            <KickOff>Kick-off&nbsp;</KickOff>
            <div>{formatInTimeZone(date, timeZone,
                timeZone === Intl.DateTimeFormat().resolvedOptions().timeZone ? "HH:mm" : "HH:mm zzz",
                { locale: enGB })}
            </div>
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

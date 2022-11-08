import styled from "styled-components";
import { theme, media } from "../../../Modules/theme";

export const Container = styled.div`
    display: flex;
    align-items: center;

    border: ${theme.border};
    background-color: ${theme.headerBackgroundColor};
    padding: 3px 0px;

    text-transform: uppercase;
    font-size: 0.9em;
    font-weight: bold;

    ${media.mobile} {
        font-size: 0.75em;
        padding: 2px 0px;
    }
`;

export const Prediction = styled.div`
    flex-grow: 0;
    width: 150px;
    text-align: center;

    ${media.mobile} {
        width: 122px;
    }
`;

export const TeamBase = styled.div`
    flex-grow: 1;
    flex-basis: 0;
`;

export const KickOffAndTeam1 = styled(TeamBase)`
    display: flex;
    justify-content: space-between;
    padding-left: 5px;
`;

export const KickOff = styled.div`
    ${media.mobile} {
        display: none;
    }
`;

export const Team1 = styled.div`
    flex: 1;
    text-align: right;
`;

export const Team2 = styled(TeamBase)`
`;

export const Status = styled.div`
    flex: 0 0 200px;
    text-align: center;

    ${media.mobile} {
        display: none;
    }
`;
export const PostMatch = styled(Status)`
    display: flex;

    ${media.mobile} {
        display: none;
    }
`;

export const Result = styled.div`
    flex: 0 0 75px;
    text-align: center;
`;

export const Points = styled.div`
    flex: 0 0 70px;
    text-align: center;
`;

import styled from "styled-components";
import { media, theme } from "../../../Modules/theme";

export const Container = styled.div`
    padding: 10px 0 15px 0;
`;

export const PredictedWinnerContainer = styled.div`
    padding: 0 20px;

    ${media.mobile} {
        font-size: 0.9em;
        padding: 0 10px;
    }
`;

export const PredictedWinnerTitle = styled.b`
    font-size: 0.9em;
`;

export const PointWinnersContainer = styled.div`
    padding-top: 15px;
`;

export const Points = styled.span`
    color: ${({ $points }) => theme.pointsColor[$points || 0]};
`;
import styled from "styled-components";
import { theme, media } from "../../../Modules/theme";

export const Container = styled.div`
    padding-bottom: 15px;
`;

export const Bars = styled.div`
    border: ${theme.border};
    background-color: ${theme.predictionBarDrawColor};
    display: flex;
    justify-content: space-between;
    margin: 5px 0;

    ${media.mobile} {
        margin: 2px 0;
    }
`;

export const Bar = styled.div`
    width: ${({ percent }) => percent}%;
    height: 10px;
    background-color: ${({ isLeft }) => isLeft ? theme.predictionBarHomeWinColor : theme.predictionBarAwayWinColor};

    ${({ isLeft }) => isLeft ? `
        margin-right: 2px;
    ` : `
        margin-left: 2px;
    `}
`;

export const TextBar = styled.div`
    display: flex;
    align-items: baseline;
    justify-content: space-between;
`;

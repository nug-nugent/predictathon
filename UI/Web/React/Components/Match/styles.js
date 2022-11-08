import styled from "styled-components";
import { media, theme } from "../../Modules/theme";

export const Container = styled.div`
    max-width: ${theme.matchListWidth};
    margin: auto;
    padding: 0 4px;

    ${({ showHeaders }) => showHeaders && `
        margin-top: 10px;
    `}
`;

export const SubContainer = styled.div`
    display: flex;
    align-items: center;

    border-bottom: ${theme.border};
    background-color: ${theme.backgroundColor};

    ${media.mobile} {
        flex-direction: column;
        align-items: stretch;
        border-bottom: 0;
        background-color: transparent;
    }
`;

import styled from "styled-components";
import { theme } from "../../Modules/theme";

export const Container = styled.div`
    margin-bottom: 10px;
`;

export const TextContainer = styled.div`
    padding: 15px;
    text-align: center;
`;
export const KnockoutWarningContainer = styled(TextContainer)`
    font-style: italic;

    & span {
        font-size: 1.2em;
        font-weight: bold;
        color: ${theme.asterickColor};
    }
`;

export const DateHeader = styled.h3`
    max-width: ${theme.matchListWidth};
    margin: auto;
    margin-top: 20px;
    padding: 0 4px;
`;

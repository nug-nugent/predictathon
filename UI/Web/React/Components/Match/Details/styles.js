import styled from "styled-components";
import { MatchStatus } from "../../../Modules/constants";
import { theme, media } from "../../../Modules/theme";

export const Container = styled.div`
    flex-grow: 1;
    display: flex;
    justify-content: stretch;
    align-items: center;
    position: relative;
    height: 35px;

    background-color: ${theme.backgroundColor};

    ${media.mobile} {
        border-left: ${theme.border};
        border-right: ${theme.border};
    }
`;

export const Team2 = styled.div`
    flex-grow: 1;
    flex-basis: 0;
    font-size: 1.4em;

    display: flex;
    align-items: center;

    ${({ $isKnockout }) => $isKnockout && `
        & div::after {
            padding-left: 2px;
            font-size: 1.4em;
            line-height: 0.1em;
            color: ${theme.asterickColor};
            content: "*"
        }
    `}

    ${media.mobile} {
        font-size: 1em;
    }
`;
export const Team1 = styled(Team2)`
    justify-content: flex-end;
    text-align: right;
`;

export const FlagBase = styled.img`
    width: 22px;
    ${media.mobile} {
        width: 18px;
    }
`;
export const Flag1 = styled(FlagBase)`
    padding-left: 10px;
    ${media.mobile} {
        padding-left: 7px;
    }
`;
export const Flag2 = styled(FlagBase)`
    padding-right: 10px;
    ${media.mobile} {
        padding-right: 7px;
    }
`;

export const Input2 = styled.input.attrs(({ matchStatus }) => ({
    type: "text",
    inputMode: "numeric",
    maxLength: 1,
    disabled: matchStatus !== MatchStatus.Pre
}))`
    flex-grow: 0;
    text-align: center;
    font-size: 1.4em;
    width: 20px;
    padding: 2px;
    margin: 0px 10px 0px 2px;
    border: ${theme.border};

    &:disabled {
        border-color: transparent;
        background-color: unset;

        // ios safari hacks
        -webkit-text-fill-color: ${theme.textColor};
        opacity: 1;
    }

    ${media.mobile} {
        margin: 0px 7px 0px 2px;
        font-size: 1.4em;
        width: 18px;
    }
`;
export const Input1 = styled(Input2)`
    margin: 0px 2px 0px 10px;
    ${media.mobile} {
        margin: 0px 2px 0px 7px;
    }
`;

export const Dash = styled.div`
    flex-grow: 0;
    text-align: center;
    font-size: 1.6em;

    ${media.mobile} {
        font-size: 1.4em;
    }
`;

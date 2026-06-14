import styled from "styled-components";
import { Icon } from "../../Modules/icons";
import { theme, media } from "../../Modules/theme";

export const Container = styled.div`
    display: table;
    width: 100%;
`;

export const Header = styled.div`
    display: table-row;
`;

export const HeaderColumn = styled.div`
    display: table-cell;
    background-color: ${theme.headerBackgroundColor};
    border-bottom: ${({ bottomBorder }) => bottomBorder ? theme.border : "unset" };
    border-right: ${({ rightBorder }) => rightBorder ? theme.border : "unset" };
    padding: 5px 20px;
    min-width: ${({ minWidth }) => minWidth || "unset"};
    width: ${({ width }) => width || "unset"};

    font-size: 0.9em;
    font-weight: bold;
    text-align: ${({ align }) => align || "left"};

    ${media.mobile} {
        width: ${({ width, mobileWidth }) => mobileWidth || width || "unset"};
        padding: 5px 10px;
        font-size: 0.75em;
    }
`;

export const Row = styled.div`
    display: table-row;
    background-color: ${theme.backgroundColor};

    &:nth-child(odd) {
        background-color: ${theme.altRowBackgroundColor};
    }
`;

export const Column = styled.div`
    display: table-cell;
    padding: 5px 20px;

    text-align: ${({ align }) => align || "left"};

    ${({ isBold }) => isBold && `
        font-weight: bold;
    `}

    ${({ lastInGroup }) => lastInGroup && `
        border-bottom: ${theme.border};
    `}

    ${media.mobile} {
        font-size: 0.9em;
        padding: 5px 10px;
    }
`;

export const Footer = styled.div`
    background-color: ${theme.headerBackgroundColor};
    border-top: ${theme.border};
    padding: 4px;
    display: flex;
    justify-content: center;
    align-items: center;
`;

export const FooterIcon = styled(Icon)`
    margin: 0 10px;
    cursor: pointer;

    ${({ disabled }) => disabled && `
        color: ${theme.footerButtonsColor};
        cursor: default;
    `}
`;

export const PageNumber = styled.div`
    border-radius: 50%;
    width: 1.8em;
    height: 1.8em;
    line-height: 1.8em;
    margin: 0 5px;
    text-align: center;
    cursor: pointer;
    
    ${({ selected }) => selected && `
        background-color: ${theme.footerButtonsColor};
        cursor: default;
        font-weight: bold;
    `}
`;

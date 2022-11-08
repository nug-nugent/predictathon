import styled from "styled-components";
import { media, theme } from "../../Modules/theme";

export const Container = styled.div`
    padding: 10px 4px 0 4px;
    display: flex;
    align-items: center;
    justify-content: center;
`;

export const ChevronContainer = styled.div`
    padding: 4px 10px;

    ${({ isRight }) => isRight ? `
        margin-left: 10px;
    ` : `
        margin-right: 10px;
    `}

    border: ${theme.border};
    border-radius: 15px;
    line-height: 0.8em;
    text-align: center;
    user-select: none;
    background-color: ${theme.buttonBgColor};
    
    ${({ enabled }) => enabled ? `
        cursor: pointer;
        color: ${theme.buttonTextColor};

        &:hover {
            background-color: ${theme.buttonHoverBgColor};
        }
    ` : `
        color: ${theme.buttonDisabledTextColor};
    `}

    & span {
        ${media.mobile} {
            display: none;
        }

        ${({ isRight }) => isRight ? `
            padding-right: 5px;
        ` : `
            padding-left: 5px;
        `}
    }

    ${media.mobile} {
        padding: 4px 15px;

        ${({ isRight }) => isRight ? `
            margin-left: 5px;
        ` : `
            margin-right: 5px;
        `}
    }
`;

export const SelectContainer = styled.div`
    ${media.mobile} {
        flex: 1;
    }
`;

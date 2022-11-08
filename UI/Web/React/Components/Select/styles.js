import styled from "styled-components";
import { Icon, icons } from "../../Modules/icons";
import { theme } from "../../Modules/theme";

export const Container = styled.div`
    position: relative;
    cursor: pointer;
    user-select: none;
`;

export const SelectElement = styled.select`
    background-color: ${theme.backgroundColor};
    color: ${theme.textColor};
    border: ${theme.border};
    border-radius: 3px;
    margin: 0;
    padding: 3px 25px 3px 5px;
    width: 100%;

    appearance: none;
    outline: none;
    -webkit-tap-highlight-color: transparent;

    &::-ms-expand {
        display: none;
    }
`;

export const SelectIcon = styled(Icon).attrs({
    icon: icons.chevronDown
})`
    position: absolute;
    right: 8px;
    top: calc(50% - 0.5em);
    pointer-events: none;
`;

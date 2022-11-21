import styled from "styled-components";
import { Icon, icons } from "../../../Modules/icons";
import { theme } from "../../../Modules/theme";

export const Container = styled.div`
    padding: 3px;
    display: flex;
    border: ${theme.reactionBorder};
    background-color: ${theme.reactionBgColor};
    color: ${theme.reactionTextColor};
    align-items: center;
    margin: 1px 0 1px 5px;
    border-radius: 5px;
    cursor: pointer;

    & img {
        width: 20px;
        height: 20px;
        padding-right: 5px;
    }

    ${({ isMe }) => isMe && `
        border: ${theme.reactionIsMeBorder};
        background-color: ${theme.reactionIsMeBgColor};
        color: ${theme.reactionIsMeTextColor};
        font-weight: bold;
    `}
`;

export const PopoverContainer = styled.div`
    border: ${theme.border};
    background-color: ${theme.backgroundColor};
    border-radius: 5px;
    box-shadow: ${theme.boxShadow};
    padding: 10px;
    width: 220px;
`;

export const PopoverHeader = styled.div`
    display: flex;
    align-items: center;
`;

export const PopoverImageContainer = styled.div`
    flex: 1;
`;

export const PopoverImage = styled.img`
    width: 37px;
    height: 37px;
`;

export const PopoverButton = styled.div`
    width: 80px;
	border: ${theme.border};
    line-height: 30px;
    text-align: center;
    padding: 0 10px;
    margin-right: 10px;
	cursor: pointer;
    user-select: none;
`;

export const PopoverCloseIcon = styled(Icon).attrs({
    icon: icons.close,
    size: "lg"
})`
    cursor: pointer;
    padding: 5px;
`;

export const PopoverTitle = styled.div`
    padding: 10px 0;
    font-size: 1.1em;
    font-weight: bold;
`;
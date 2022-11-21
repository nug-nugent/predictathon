import styled from "styled-components";
import { media, theme } from "../../Modules/theme";

export const Container = styled.div`
    display: flex;
    flex-flow: column;
    width: 1005;
    height: 100%;
`;

export const TitleContainer = styled.div`
    max-width: ${theme.messageListWidth};
    margin: auto;
    padding: 10px 0;
    display: flex;
    align-items: flex-start;
    & a {
        margin-left: 4px;
        margin-right: 8px;
        text-decoration: none;
    }
`;

export const Title = styled.h1`
    flex: 1;
`;

export const BoardButton = styled.div`
    padding: 4px 8px;

    border: ${theme.border};
    border-radius: 15px;
    line-height: 0.8em;
    text-align: center;
    user-select: none;
    background-color: ${theme.buttonBgColor};
    
    cursor: pointer;
    color: ${theme.buttonTextColor};

    &:hover {
        background-color: ${theme.buttonHoverBgColor};
    }

    & span {
        padding-left: 5px;
    }
`;

export const MessagesContainer = styled.div`
    margin: auto;
`;

export const LoadMoreButton = styled(BoardButton)`
    width: 200px;
    margin: 10px auto;

    ${({ newMessagesPosted }) => newMessagesPosted && `
        background-color: ${theme.liveMessagesBgColor};
        color: ${theme.liveMessagesTextColor};
        font-weight: bold;

        &:hover {
            background-color: ${theme.liveMessagesHoverBgColor};
        }
    `}
`;

export const Separator = styled.div`
    display: flex;
    align-items: center;
    max-width: ${theme.messageListWidth};
    margin: auto;
    margin-bottom: ${theme.messageBottomMargin};
    color: ${theme.unreadSeparatorColor};
    font-weight: bold;
    padding: 2px 0;

    &::before, &::after {
        content: "";
        height: 1px;
        background-color: ${theme.unreadSeparatorColor};
        flex-grow: 1;
    }

    &::before {
        margin-right: 6px;
    }
    &::after {
        margin-left: 6px;
    }

    ${media.mobile} {
        margin-left: 4px;
        margin-right: 4px;
    }
`;
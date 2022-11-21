import Popup from "reactjs-popup";
import styled from "styled-components";
import addReactionImage from "../../Content/Images/add-reaction.svg";
import { Icon } from "../../Modules/icons";
import { media, theme } from "../../Modules/theme";

export const Container = styled.div`
    max-width: ${theme.messageListWidth};
    margin: auto;
    padding: 0 4px;
    margin-bottom: ${theme.messageBottomMargin};
    display: flex;
    align-items: flex-start;
`;

export const ProfileImageContainer = styled.div`
    padding-right: 8px;
    flex: 0 0 ${theme.messageProfileImgSize};
`;

export const ProfileImage = styled.img`
    width: ${theme.messageProfileImgSize};
    height: ${theme.messageProfileImgSize};
    border-radius: 50%;
    object-fit: cover;
`;

export const MessageContainer = styled.div`
    flex: 1;
    border-bottom: ${theme.border};
`;

export const HeaderContainer = styled.div`
    background-color: ${theme.messageHeaderBgColor};
    color: ${theme.messageHeaderTextColor};
    border-top-left-radius: 10px;
    border-top-right-radius: 10px;
    padding: 3px;
    overflow: auto;
`;

export const AuthorLink = styled.a`
    font-weight: bold;
    text-decoration: none;
    padding: 8px 5px 6px 3px;
    float: left;
`;

export const DateContainer = styled.div`
    float: left;
    padding: 8px 5px 6px 3px;
`;

export const ReactionsContainer = styled.div`
    float: right;
`;

export const AddReactionIcon = styled.img.attrs({
    src: addReactionImage
})`
    width: 26px;
    margin: 2px 4px 2px 8px;
    cursor: pointer;
    float: right;
`;

export const MediaContainer = styled.div`
    margin-left: 5px;
    float: right;
    position: relative;

    & img {
        cursor: pointer;
        width: 300px;
        height: 300px;
        object-fit: cover;
    }

    & iframe {
        max-width: 100%;
    	width: 400px;
	    height: 225px;
    }

    ${media.mobile} {
        margin-left: 0px;
		float: none;

        & img {
            width: 100%;
            height: unset;
            aspect-ratio: 1 / 1;
        }

        & iframe {
            height: 180px;
        }
    }
`;

export const CloseIcon = styled(Icon)`
    color: ${theme.backgroundColor};
    position: absolute;
    top: 3px;
    right: 1px;
    cursor: pointer;
`;

export const ImagePopup = styled(Popup)`
    &-overlay {
        background-color: rgba(70,70,70,0.7);
        pointer-events: none;
    }

    &-content {
        cursor: pointer;
        position: relative;

        & img {
            max-width: 100vw;
            max-height: 100vh;
        }
    }
`;

export const TextContainer = styled.div`
    background-color: ${theme.backgroundColor};
    clear: both;
    word-break: break-word;
`;

export const MarkdownContainer = styled.div`
    padding: 5px;
    
    p:first-child {
        margin-top: 0;
    }

    p:last-child {
        margin-bottom: 0;
    }
`;

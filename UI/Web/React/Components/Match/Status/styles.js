import styled from "styled-components";
import { MatchStatus } from "../../../Modules/constants";
import { theme, media } from "../../../Modules/theme";
import { Icon, icons } from "../../../Modules/icons";

export const statusStyles = {
    pre: { iconColor: theme.infoIconColor, icon: theme.infoIcon, iconOnmobile: false, mobileBgColor: theme.infoMobileBgColor, mobileColor: theme.textColor },
    warning: { iconColor: theme.warningIconColor, icon: theme.warningIcon, iconOnmobile: false, mobileBgColor: theme.warningMobileBgColor, mobileColor: theme.textInverseColor },
    during: { iconColor: theme.infoIconColor, icon: theme.infoIcon, iconOnmobile: false, mobileBgColor: theme.duringMatchMobileBgColor, mobileColor: theme.textColor },
    saving: { iconColor: theme.infoIconColor, icon: theme.savingIcon, iconOnmobile: true, pulse: true, mobileBgColor: theme.infoMobileBgColor, mobileColor: theme.textColor },
    savingFromWarning: { iconColor: theme.infoIconColor, icon: theme.savingIcon, iconOnmobile: true, pulse: true, mobileBgColor: theme.warningMobileBgColor, mobileColor: theme.textInverseColor },
    saved: { iconColor: theme.savedIconColor, icon: theme.savedIcon, iconOnmobile: true, mobileBgColor: theme.savedMobileBgColor, mobileColor: theme.textInverseColor },
    error: { iconColor: theme.saveErrorIconColor, icon: theme.saveErrorIcon, iconOnmobile: true, mobileBgColor: theme.saveErrorMobileBgColor, mobileColor: theme.textInverseColor }
};

export const Container = styled.div`
    flex: 0 0 200px;
    background-color: ${theme.backgroundColor};

    ${({ matchStatus }) => matchStatus !== MatchStatus.Pre && `
        display: flex;
        align-items: center;
    `}

    ${media.mobile} {
        flex: 1;
        text-align: center;
        padding: 5px;

        background-color: ${theme.statusMobileBgColor};
        border: ${theme.border};
        border-top: 0;

        ${({ lastInGroup }) => lastInGroup && `
            border-bottom-left-radius: 25px;
            border-bottom-right-radius: 25px;
        `}

        ${({ matchStatus, statusStyle }) => matchStatus !== MatchStatus.Post && `
            background-color: ${statusStyle.mobileBgColor};
            color: ${statusStyle.mobileColor};;
        `}

        ${({ matchStatus }) => matchStatus == MatchStatus.During && `
            display: flex;
            justify-content: center;
        `}

        ${({ matchStatus }) => matchStatus == MatchStatus.Post && `
            display: flex;
            justify-content: space-between;
            font-weight: bold;
            text-transform: uppercase;
        `}
    }
`;

export const StatusIcon = styled(Icon)`
    ${media.mobile} {
        ${({ $showOnMobile, $mobileColor }) => $showOnMobile ? `
            color: ${$mobileColor};
        ` : `
            display: none;
        `}
    }
`;

export const ResultBase = styled.div`
    font-size: 1.6em;
    text-align: center;

    & div {
        display: none;
    }

    ${media.mobile} {
        font-size: 0.9em;

        & div {
            display: inline-block;
        }
    }
`;
export const Result = styled(ResultBase)`
    flex: 0 0 75px;

    ${media.mobile} {
        flex: 0 0 90px;
        text-align: right;
    }
`;
export const Points = styled(ResultBase)`
    flex: 0 0 70px;

    color: ${({ $points }) => theme.pointsColor[$points || 0]};

    ${media.mobile} {
        flex: 0 0 90px;
        order: 1;
        text-align: left;
    }
`;

export const StatusTextContainer = styled.div`
    ${({ matchStatus }) => matchStatus == MatchStatus.During && `
        flex: 0 0 145px;

        ${media.mobile} {
            display: none;
        }
    `}
`;

export const PredictionsToggle = styled.div`
    flex: 0 0 40px;
    padding: 4px;
    border: ${theme.border};
    border-radius: 15px;
    text-align: center;
    cursor: pointer;
    user-select: none;
    
    &:hover {
        background-color: ${theme.buttonHoverBgColor};
    }

    & span {
        display: none;
    }
    
    ${media.mobile} {
        font-size: 0.9em;
        padding: unset;
        text-transform: uppercase;
        flex: unset;
        border: unset;

        &:hover {
            background-color: unset;
        }

        & span {
            display: inline;
            font-weight: bold;
        }
    }
`;

export const PredictionsIcon = styled(Icon).attrs({
    icon: icons.list
})`
    padding-right: 5px;

    ${media.mobile} {
        display: none;
    }
`;

export const PredictionsContainer = styled.div`
    margin-top: 3px;
    margin-bottom: 5px;
    border: ${theme.border};
    box-shadow: ${theme.boxShadow};
`;

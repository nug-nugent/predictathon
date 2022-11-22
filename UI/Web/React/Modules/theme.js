import { colors } from './colors';
import { icons } from './icons';

const sizes = {
    mobile: '600px'
};

const baseTheme = {
    // general
    backgroundColor: colors.white,
    textColor: colors.black,
    textInverseColor: colors.white,

    border: `1px solid ${colors.grey}`,
    boxShadow: `rgba(0, 0, 0, 0.3) 0px 5px 10px`,

    // table

    headerBackgroundColor: colors.lightGrey,
    altRowBackgroundColor: colors.lighterGrey,
    footerButtonsColor: colors.lightBlue,

    // button
    
    buttonBgColor: colors.white,
    buttonTextColor: colors.black,
    buttonDisabledTextColor: colors.grey,
    buttonHoverBgColor: colors.lighterGrey,
    
    // match

    matchListWidth: "850px",

    predictionsListButtonColor: colors.black,
    predictionsListButtonErrorColor: colors.darkRed,
    predictionsListButtonExpand: icons.chevronDown,
    predictionsListButtonCollapse: icons.chevronUp,
    predictionsListButtonLoading: icons.loading,
    predictionsListButtonError: icons.error,

    asterickColor: colors.red,

    infoIcon: icons.info,
    infoIconColor: colors.blue,
    infoMobileBgColor: colors.lightBlue,

    warningIcon: icons.warning,
    warningIconColor: colors.orange,
    warningMobileBgColor: colors.orange,

    savingIcon: icons.loading,
    savedIcon: icons.check,
    savedIconColor: colors.green,
    savedMobileBgColor: colors.green,

    saveErrorIcon: icons.error,
    saveErrorIconColor: colors.darkRed,
    saveErrorMobileBgColor: colors.darkRed,

    duringMatchMobileBgColor: colors.lightGrey,

    statusMobileBgColor: colors.lightGrey,

    pointsColor: Object.freeze({
        0: colors.red,
        1: colors.darkOrange,
        2: colors.darkGreen,
        3: colors.green
    }),

    // message board

    messageListWidth: "850px",
    messageBottomMargin: "5px",
    messageProfileImgSize: "60px",
    messageHeaderBgColor: colors.lightGrey,
    messageHeaderTextColor: colors.darkGrey,

    reactionBgColor: colors.white,
    reactionTextColor: colors.darkGrey,
    reactionBorder: `1px solid ${colors.white}`,
    reactionIsMeBgColor: colors.white,
    reactionIsMeTextColor: colors.black,
    reactionIsMeBorder: `1px solid ${colors.grey}`,

    unreadSeparatorColor: colors.red,

    liveMessagesBgColor: colors.darkBlue,
    liveMessagesHoverBgColor: colors.darkerBlue,
    liveMessagesTextColor: colors.white,
    liveErrorIconColor: colors.darkRed
};

export const theme = Object.freeze(baseTheme);

export const media = Object.freeze({
    mobile: `@media handheld and (orientation: portrait), screen and ( max-width: ${sizes.mobile} )`
});

import { colors } from './colors';
import { icons } from './icons';

const sizes = {
    mobile: '600px'
};

const baseTheme = {
    backgroundColor: colors.white,
    textColor: colors.black,
    textInverseColor: colors.white,

    border: `1px solid ${colors.grey}`,
    boxShadow: `rgba(0, 0, 0, 0.3) 0px 5px 10px`,

    matchListWidth: "850px",

    headerBackgroundColor: colors.lightGrey,
    altRowBackgroundColor: colors.lighterGrey,
    footerButtonsColor: colors.lightBlue,

    buttonBgColor: colors.white,
    buttonTextColor: colors.black,
    buttonDisabledTextColor: colors.grey,
    buttonHoverBgColor: colors.lighterGrey,

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
    })
};

export const theme = Object.freeze(baseTheme);

export const media = Object.freeze({
    mobile: `@media handheld and (orientation: portrait), screen and ( max-width: ${sizes.mobile} )`
});

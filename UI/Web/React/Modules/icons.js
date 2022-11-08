import { faAngleDown, faAngleLeft, faAngleRight, faAnglesLeft, faAnglesRight, faAngleUp, faArrowLeft, faArrowRight, faCheckCircle, faExclamationCircle, faExclamationTriangle, faInfoCircle, faList, faSpinner } from "@fortawesome/free-solid-svg-icons";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";

export const icons = Object.freeze({
    loading: faSpinner,
    check: faCheckCircle,

    info: faInfoCircle,
    warning: faExclamationTriangle,
    error: faExclamationCircle,
    
    list: faList,
    chevronDown: faAngleDown,
    chevronUp: faAngleUp,
    chevronLeft: faAngleLeft,
    chevronRight: faAngleRight,
    chevronDoubleLeft: faAnglesLeft,
    chevronDoubleRight: faAnglesRight,

    arrowLeft: faArrowLeft,
    arrowRight: faArrowRight
});

export const Icon = FontAwesomeIcon;

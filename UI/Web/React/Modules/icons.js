import { faAngleDown, faAngleLeft, faAngleRight, faAnglesLeft, faAnglesRight, faAngleUp, faArrowDown, faArrowLeft, faArrowRight, faArrowUp, faCheckCircle, faExclamationCircle, faExclamationTriangle, faInfoCircle, faList, faSpinner, faXmark } from "@fortawesome/free-solid-svg-icons";
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

    arrowDown: faArrowDown,
    arrowUp: faArrowUp,
    arrowLeft: faArrowLeft,
    arrowRight: faArrowRight,

    close: faXmark
});

export const Icon = FontAwesomeIcon;

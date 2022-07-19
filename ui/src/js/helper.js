import * as Config from './config.js';

export function getUrlParameters() {
    return new URLSearchParams(window.location.search);
}

function dateToObj(rawTime, dataStartDate) {
    return new Date(dataStartDate.getTime() + rawTime * 60 * 1000);
}

export function parseTime(rawTime, dataStartDate) {
    const dateObj = dateToObj(rawTime, dataStartDate);
    return dateObj.toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' });
}

export function parseTimeSpan(rawTimeSpan) {
    const hoursStr = toTwoDigitFormat(Math.floor(Math.abs(rawTimeSpan) / 60), rawTimeSpan < 0);
    const minutesStr = toTwoDigitFormat(Math.abs(rawTimeSpan % 60));
    return `${hoursStr}:${minutesStr}`;
}

function toTwoDigitFormat(number, isNegative = false) {
    const numberAbs = Math.abs(number);
    let numberStr = numberAbs < 10 ? '0' + numberAbs : numberAbs;
    if (isNegative) numberStr = '-' + numberStr;
    return numberStr;;
}

export function parseTimeDiff(rawTime1, rawTime2) {
    return parseTimeSpan(rawTime2 - rawTime1);
}

export function parseDate(rawTime, dataStartDate) {
    const dateObj = dateToObj(rawTime, dataStartDate);
    return dateObj.toDateString();
}

export function parseDateShort(rawTime, dataStartDate) {
    const dateObj = dateToObj(rawTime, dataStartDate);
    return dateObj.toLocaleDateString('en-GB', { weekday: 'short', day: 'numeric', month: 'numeric' });
}

export function parseDayOfWeek(rawTime, dataStartDate) {
    const dateObj = dateToObj(rawTime, dataStartDate);
    return dateObj.toLocaleDateString('en-GB', { weekday: 'short' });
}

export function infoObjectToRows(infoObj) {
    return Object.keys(infoObj).map(key => {
        if (infoObj[key] == null) return '';

        let classes = 'infoRow';
        let labelBefore = '';
        let parsedKey = key;
        if (parsedKey.substring(0, 1) === '>') {
            parsedKey = parsedKey.substring(1);
            labelBefore = '<i class="icon fa-solid fa-arrow-right"></i>';
            classes += ' subRow';
        }

        return `
            <div class="${classes}">
                <span class="label">${labelBefore}${this.camelCaseToWords(parsedKey)}</span>
                <span class="value">${infoObj[key]}</span>
            </div>
        `;
    });
}

export function camelCaseToWords(camelCaseStr) {
    let result = camelCaseStr.replace(/([A-Z])/g, " $1");
    result = result.toLowerCase();
    result = result.charAt(0).toUpperCase() + result.slice(1);
    return result;
}
import * as Config from '../shared/config.js';

function toDateObj(rawTime) {
    return new Date(Config.startDate.getTime() + rawTime * 60 * 1000);
}

export function parseTime(rawTime) {
    const dateObj = toDateObj(rawTime);
    return dateObj.toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' });
}

export function parseTimeDiff(rawTime1, rawTime2) {
    const dateObj1 = toDateObj(rawTime1);
    const dateObj2 = toDateObj(rawTime2);
    const isNegative = dateObj1 > dateObj2;
    const dateDiffObj = new Date(Math.abs(dateObj2 - dateObj1));
    let dateDiffStr = dateDiffObj.toLocaleTimeString('en-GB', { timeZone: 'UTC', hour: '2-digit', minute: '2-digit' });
    if (isNegative) dateDiffStr = '-' + dateDiffStr;
    return dateDiffStr;
}

export function parseDate(rawTime) {
    const dateObj = toDateObj(rawTime);
    return dateObj.toDateString();
}

export function parseDateShort(rawTime) {
    const dateObj = toDateObj(rawTime);
    return dateObj.toLocaleDateString('en-GB', { weekday: 'short', day: 'numeric', month: 'numeric' });
}

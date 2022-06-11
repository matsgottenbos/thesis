import * as Config from '../shared/config.js';

function toDateObj(rawTime) {
    return new Date(Config.startDate.getTime() + rawTime * 60 * 1000);
}

export function parseTime(rawTime) {
    const time = toDateObj(rawTime);
    let hoursStr = time.getHours().toString();
    if (hoursStr.length < 2) hoursStr = "0" + hoursStr;
    let minsStr = time.getMinutes().toString();
    if (minsStr.length < 2) minsStr = "0" + minsStr;
    return `${hoursStr}:${minsStr}`;
}

export function parseTimeDiff(rawTime1, rawTime2) {
    const timeDiff = rawTime2 - rawTime1;
    let hoursStr = Math.floor(timeDiff / 60).toString();
    if (hoursStr.length < 2) hoursStr = "0" + hoursStr;
    let minsStr = (timeDiff % 60).toString();
    if (minsStr.length < 2) minsStr = "0" + minsStr;
    return `${hoursStr}:${minsStr}`;
}

export function parseDate(rawTime) {
    const time = toDateObj(rawTime);
    return time.toDateString();
}

export function parseDateShort(rawTime) {
    const time = toDateObj(rawTime);
    return time.toLocaleDateString('en-GB', { weekday: 'short', day: 'numeric', month: 'numeric' });
}

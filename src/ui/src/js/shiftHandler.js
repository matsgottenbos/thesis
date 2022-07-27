import * as Helper from './helper.js';
import * as Config from './config.js';

export function getShiftHtml(shift, shiftIndex, driver, dataStartDate) {
    const driverName = Config.showRealDriverNames ? driver.realDriverName : driver.driverName;
    const currentShiftDateStr = `${Helper.parseDate(shift.activityPath[0].startTime, dataStartDate)} ${Helper.parseTime(shift.activityPath[0].startTime, dataStartDate)}`;

    const shiftHtmlParts = [];
    shift.activityPath.forEach(item => {
        const startTimeStr = Helper.parseTime(item.startTime, dataStartDate);
        const endTimeStr = Helper.parseTime(item.endTime, dataStartDate);
        const durationStr = Helper.parseTimeDiff(item.startTime, item.endTime);

        const activityInfo = {
            id: '',
            startTime: startTimeStr,
            endTime: endTimeStr,
            duration: durationStr,
            duty: null,
            activity: null,
            project: null,
            train: null,
            combinedDetails: null,
            fromLocation: '',
            toLocation: '',
        };

        if (item.type === 'activity') {
            activityInfo.id = item.activityIndex;
            activityInfo.duty = item.dutyName;
            activityInfo.activity = item.activityName;
            activityInfo.project = item.projectName;
            activityInfo.train = item.trainNumber;
            activityInfo.fromLocation = item.startStationName;
            activityInfo.toLocation = item.endStationName;
        } else {
            let name;
            if (item.type === 'travelBetween') name = 'Travel between activities (shared car)';
            else if (item.type === 'wait') name = `Waiting (expected delay cost: &euro; ${Math.round(item.robustness)})`;
            else if (item.type === 'travelFromHome') name = 'Travel from home (personal car)';
            else if (item.type === 'travelToCar') name = 'Travel to personal car (shared car)';
            else if (item.type === 'travelToHome') name = 'Travel to home (personal car)';
            else if (item.type === 'travelToHotel') name = 'Travel to hotel (shared car)';
            else if (item.type === 'travelFromHotel') name = 'Travel from hotel (shared car)';
            else if (item.type === 'rest') name = 'Rest';
            else if (item.type === 'hotel') name = 'Hotel stay';
            else if (item.type === 'overlapError') name = 'Overlap error';

            activityInfo.combinedDetails = name;

            if (item.type == 'travelBetween' || item.type == 'travelToCar' || item.type == 'travelToHome' || item.type == 'travelToHotel') {
                activityInfo.fromLocation = item.startStationName;
            }
            if (item.type == 'travelBetween' || item.type == 'travelToCar' || item.type == 'travelFromHome' || item.type == 'travelFromHotel') {
                activityInfo.toLocation = item.endStationName;
            }

            if (item.type == 'wait' && item.endTime - item.startTime === 0) return;
        }

        if (!Config.shouldShowActivityIds) {
            delete activityInfo.id;
        }

        const activityCellHtmls = Object.keys(activityInfo).map(key => {
            if (activityInfo[key] === null) return '';
            return `<span class="cell ${key}">${activityInfo[key]}</span>`;
        });
        const activityHtml = `<div class="row pathItem ${item.type}">${activityCellHtmls.join('')}</div>`;
        shiftHtmlParts.push(activityHtml);
    });

    return getShiftHtmlFromInfo(shiftIndex, shift, shiftHtmlParts, driverName, currentShiftDateStr, dataStartDate);
}


function getShiftHtmlFromInfo(shiftIndex, shift, shiftHtmlParts, driverName, currentShiftDateStr, dataStartDate) {
    let shiftLengthStr = Helper.parseTimeSpan(shift.realMainShiftLength);
    if (shift.paidMainShiftLength >= shift.realMainShiftLength + 15) {
        shiftLengthStr += `, counted as minimum of ${Helper.parseTimeSpan(shift.paidMainShiftLength)}`;
    }

    const shiftInfo = {
        driver: driverName,
        cost: '&euro; ' + Math.round(shift.cost),
        '>shiftSalary': '&euro; ' + Math.round(shift.mainShiftCost),
        '>travelSalary': '&euro; ' + Math.round(shift.travelCost),
        '>sharedCarTravelCost': '&euro; ' + Math.round(shift.sharedCarTravelCost),
        '>hotelCost': '&euro; ' + Math.round(shift.hotelCost),
        '>expectedDelayCost': '&euro; ' + Math.round(shift.robustness),
        '>penaltyCost': shift.penalty > 0 ? '&euro; ' + Math.round(shift.penalty) : null,
        shiftStartTime: Helper.parseTime(shift.mainShiftStartTime, dataStartDate),
        shiftEndTime: Helper.parseTime(shift.realMainShiftEndTime, dataStartDate),
        shiftLength: shiftLengthStr,
        sharedCarTravelBeforeShift: getTimeAndDistanceStr(shift.sharedCarTravelTimeBefore, shift.sharedCarTravelDistanceBefore),
        ownCarTravelBeforeShift: getTimeAndDistanceStr(shift.ownCarTravelTimeBefore, shift.ownCarTravelDistanceBefore),
        sharedCarTravelAfterShift: getTimeAndDistanceStr(shift.sharedCarTravelTimeAfter, shift.sharedCarTravelDistanceAfter),
        ownCarTravelAfterShift: getTimeAndDistanceStr(shift.ownCarTravelTimeAfter, shift.ownCarTravelDistanceAfter),
    };
    const shiftInfoHtmlParts = Helper.infoObjectToRows(shiftInfo);

    const salaryRatesHtmlParts = shift.salaryRates.map(rate => `
        <div class="row">
            <span class="cell activityTime">${Helper.parseDayOfWeek(rate.salaryStartTime, dataStartDate)} ${Helper.parseTime(rate.salaryStartTime, dataStartDate)} - ${Helper.parseTime(rate.salaryEndTime, dataStartDate)}</span>
            <span class="cell activityDuration">${Helper.parseTime(rate.salaryDuration, dataStartDate)}</span>
            <span class="cell rateBlock">${Helper.parseDayOfWeek(rate.rateStartTime, dataStartDate)} ${Helper.parseTime(rate.rateStartTime, dataStartDate)} - ${Helper.parseTime(rate.rateEndTime, dataStartDate)}</span>
            <span class="cell hourlyRate">${rate.usesContinuingRate ? 'Continuing: ' : ''}&euro; ${rate.hourlySalaryRate}</span>
            <span class="cell salaryAmount">&euro; ${Math.round(rate.shiftCostInRange)}</span>
        </div>
    `);

    return (`
        <div class="shift">
            <h2 class="shiftHeader">Shift ${shiftIndex + 1}: ${currentShiftDateStr}</h2>
            <div class="shiftInfo category">
                <h3 class="categoryHeader">Shift info</h3>
                <div class="rows">${shiftInfoHtmlParts.join('')}</div>
            </div>
            <div class="shiftSalaryRatesInfo category">
                <h3 class="categoryHeader">Salary rates</h3>
                <div class="categoryDescription">NB: times are rounded to 15 minutes.</div>
                <div class="row shiftSalaryRatesInfoHeaderRow">
                    <span class="cell activityTime">Activity part</span>
                    <span class="cell activityDuration">Part duration</span>
                    <span class="cell rateBlock">Salary rate block</span>
                    <span class="cell hourlyRate">Hourly rate</span>
                    <span class="cell salaryAmount">Salary amount</span>
                </div>
                <div class="shiftSalaryRatesInfoRows">${salaryRatesHtmlParts.join('')}</div>
            </div>
            <div class="shiftPath category">
                <h3 class="categoryHeader">Shift schedule</h3>
                <div class="row shiftPathHeaderRow">
                    ${Config.shouldShowActivityIds ? '<span class="cell id">Act. ID</span>' : ''}
                    <span class="cell startTime">Start</span>
                    <span class="cell endTime">End</span>
                    <span class="cell duration">Length</span>
                    <span class="cell duty">Duty</span>
                    <span class="cell activity">Activity</span>
                    <span class="cell project">Project</span>
                    <span class="cell train">Train</span>
                    <span class="cell fromLocation">From location</span>
                    <span class="cell toLocation">To location</span>
                </div>
                <div class="shiftPathRows">${shiftHtmlParts.join('')}</div>
            </div>
        </div>
    `);
}

function getTimeAndDistanceStr(rawTimeSpan, distance) {
    if (rawTimeSpan == 0 && distance == 0) return '-';
    const timeStr = Helper.parseTimeSpan(rawTimeSpan);
    return `${timeStr}, ${distance} km`;
}

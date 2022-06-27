import * as Api from './api.js';
import * as Helper from './helper.js';
import * as Config from './config.js';

class DriverPage {
    data = null;

    async init() {
        const urlParameters = Helper.getUrlParameters();

        const selectedScheduleName = urlParameters.get('schedule');
        if (selectedScheduleName == null) {
            window.location = '../';
        }

        const selectDriverStr = urlParameters.get('driver');
        if (selectDriverStr == null) {
            urlParameters.set('driver', 1);
            window.location.search = '?' + urlParameters.toString();
            return;
        }

        this.data = await Api.getData(selectedScheduleName, '../');

        $('.backButton').click(() => {
            window.location = `../schedule/?schedule=${selectedScheduleName}`;
        });

        const selectedDriverIndex = parseInt(selectDriverStr) - 1;
        const driver = this.data.drivers[selectedDriverIndex];

        const name = this.showRealDriverNames ? driver.realDriverName : driver.driverName;
        $('.pageTitle').html(name);

        // Driver info
        const driverDetails = {
            cost: '&euro; ' + Math.round(driver.stats.cost),
            '>costWithoutDelays': '&euro; ' + Math.round(driver.stats.rawCost),
            '>expectedDelayCost': '&euro; ' + Math.round(driver.stats.robustness),
            satisfaction: Math.round(driver.stats.driverSatisfaction * 100) + '%',
        };
        const driverDetailsHtmlParts = Helper.infoObjectToRows(driverDetails);
        $('.driverDetails .stats .rows').html(driverDetailsHtmlParts.join(''));

        if (driver.isInternal) {
            const driverSatisfactionCriteria = {
                ...driver.stats.driverSatisfactionCriteria,
            };

            const driverSatisfactionCriteriaHtmlParts = Object.keys(driverSatisfactionCriteria).map(key => {
                const label = key === 'robustness' ? 'expectedDelays' : key;
                const value = Math.round(driverSatisfactionCriteria[key] * 100);
                return `
                    <div class="infoRow">
                        <span class="label">${Helper.camelCaseToWords(label)}</span>
                        <span class="value">${value}%</span>
                    </div>
                `;
            });
            $('.driverDetails .satisfactionCriteria').html(`
                <h2 class="categoryHeader">Satisfaction criteria</h2>
                <div class="rows">${driverSatisfactionCriteriaHtmlParts.join('')}</div>
            `);
        }

        const driverInfo = {
            contractTime: driver.isInternal ? Helper.parseTimeSpan(driver.contractTime) : null,
            'internal / external': driver.isInternal ? 'Internal' : 'External',
            'national / international': driver.isInternational ? 'International' : 'National',
        };

        const driverInfoHtmlParts = Helper.infoObjectToRows(driverInfo);
        $('.driverDetails .driverInfo .rows').html(driverInfoHtmlParts.join(''));

        const scheduleInfo = {
            workedTime: Helper.parseTimeSpan(driver.info.workedTime),
            travelTime: Helper.parseTimeSpan(driver.info.travelTime),
            numberOfShifts: driver.info.shiftCount,
            numberOfRepeatedRoutes: driver.info.duplicateRouteCount,
            numberOfHotelStays: driver.info.hotelCount,
            numberOfNightShifts: driver.info.nightShiftCountByCompanyRules,
            numberOfWeekendShifts: driver.info.weekendShiftCountByCompanyRules,
            numberOfSingleFreeDays: driver.info.singleFreeDayCount,
            numberOfDoubleFreeDays: driver.info.doubleFreeDayCount,
        };

        const scheduleInfoHtmlParts = Helper.infoObjectToRows(scheduleInfo);
        $('.driverDetails .driverScheduleInfo .rows').html(scheduleInfoHtmlParts.join(''));
        

        // Driver path
        const shiftsHtmlParts = [];
        let shiftHtmlParts = [];
        let currentShiftDateStr = null;
        driver.shifts.forEach(shift => {
            shift.tripPath.forEach(item => {
                if (currentShiftDateStr === null) currentShiftDateStr = Helper.parseDate(item.startTime);
                const startTimeStr = Helper.parseTime(item.startTime);
                const endTimeStr = Helper.parseTime(item.endTime);
                const durationStr = Helper.parseTimeDiff(item.startTime, item.endTime);

                if (item.type === 'trip') {
                    shiftHtmlParts.push(`
                    <div class="row pathItem ${item.type}">
                        ${Config.shouldShowActivityIds ? `<span class="cell id">${item.tripIndex}</span>` : ''}
                        <span class="cell startTime">${startTimeStr}</span>
                        <span class="cell endTime">${endTimeStr}</span>
                        <span class="cell duration">${durationStr}</span>
                        <span class="cell duty">${item.dutyName}</span>
                        <span class="cell activity">${item.activityName}</span>
                        <span class="cell fromLocation">${item.startStationName}</span>
                        <span class="cell toLocation">${item.endStationName}</span>
                    </div>
                `);
                } else {
                    let description;
                    if (item.type === 'travelBetween') description = 'Travel between activities';
                    else if (item.type === 'wait') description = `Waiting (expected delay cost: &euro; ${Math.round(item.robustness)})`;
                    else if (item.type === 'travelBefore') description = 'Travel from home';
                    else if (item.type === 'travelAfter') description = 'Travel to home';
                    else if (item.type === 'travelBeforeHotel') description = 'Travel to hotel';
                    else if (item.type === 'travelAfterHotel') description = 'Travel from hotel';
                    else if (item.type === 'rest') description = 'Rest';
                    else if (item.type === 'hotel') description = 'Hotel stay';
                    else if (item.type === 'overlapError') description = 'Overlap error';

                    if (item.type !== 'wait' || item.endTime - item.startTime > 0) {
                        shiftHtmlParts.push(`
                            <div class="row pathItem ${item.type}">
                                ${Config.shouldShowActivityIds ? '<span class="cell id"></span>' : ''}
                                <span class="cell startTime">${startTimeStr}</span>
                                <span class="cell endTime">${endTimeStr}</span>
                                <span class="cell duration">${durationStr}</span>
                                <span class="cell name">${description}</span>
                            </div>
                        `);
                    }
                }
            });

            shiftsHtmlParts.push(this.getShiftHtml(shiftsHtmlParts.length, shift, shiftHtmlParts, currentShiftDateStr));
            shiftHtmlParts = [];
            currentShiftDateStr = null;
        });

        $('.driverPath').html(shiftsHtmlParts.join(''));
    }

    getShiftHtml(shiftIndex, shift, shiftHtmlParts, currentShiftDateStr) {
        let shiftLengthStr = Helper.parseTimeSpan(shift.drivingTime);
        if (shift.administrativeDrivingTime > shift.drivingTime) {
            shiftLengthStr += `, counted as minimum of ${Helper.parseTimeSpan(shift.administrativeDrivingTime)}`;
        }

        const shiftInfo = {
            cost: '&euro; ' + Math.round(shift.cost),
            '>shiftSalary': '&euro; ' + Math.round(shift.drivingCost),
            '>travelSalary': '&euro; ' + Math.round(shift.travelCost),
            '>hotelCost': '&euro; ' + Math.round(shift.hotelCost),
            '>expectedDelayCost': '&euro; ' + Math.round(shift.robustness),
            '>penaltyCost': shift.penalty > 0 ? '&euro; ' + Math.round(shift.penalty) : null,
            shiftLength: shiftLengthStr,
            shiftStartTime: Helper.parseTime(shift.startTime),
            shiftEndTime: Helper.parseTime(shift.administrativeEndTime),
            travelTimeBeforeShift: Helper.parseTimeSpan(shift.travelTimeBefore),
            travelDistanceBeforeShift: shift.travelDistanceBefore + ' km',
            travelTimeAfterShift: Helper.parseTimeSpan(shift.travelTimeAfter),
            travelDistanceAfterShift: shift.travelDistanceAfter + ' km',
        };
        const shiftInfoHtmlParts = Helper.infoObjectToRows(shiftInfo);

        const salaryRatesHtmlParts = shift.salaryRates.map(rate => `
            <div class="row">
                <span class="cell activityTime">${Helper.parseDayOfWeek(rate.salaryStartTime)} ${Helper.parseTime(rate.salaryStartTime)} - ${Helper.parseTime(rate.salaryEndTime)}</span>
                <span class="cell activityTime">${Helper.parseTime(rate.salaryDuration)}</span>
                <span class="cell rateBlock">${Helper.parseDayOfWeek(rate.rateStartTime)} ${Helper.parseTime(rate.rateStartTime)} - ${Helper.parseTime(rate.rateEndTime)}</span>
                <span class="cell hourlyRate">${rate.usesContinuingRate ? 'Continuing: ' : ''}&euro; ${rate.hourlySalaryRate}</span>
                <span class="cell salaryAmount">&euro; ${Math.round(rate.drivingCostInRange)}</span>
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
                    <div class="row shiftSalaryRatesInfoHeaderRow">
                        <span class="cell activityTime">Activity part</span>
                        <span class="cell activityTime">Part duration</span>
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
                        <span class="cell startTime">Start time</span>
                        <span class="cell endTime">End time</span>
                        <span class="cell duration">Duration</span>
                        <span class="cell duty">Duty</span>
                        <span class="cell activity">Activity</span>
                        <span class="cell fromLocation">From location</span>
                        <span class="cell toLocation">To location</span>
                    </div>
                    <div class="shiftPathRows">${shiftHtmlParts.join('')}</div>
                </div>
            </div>
        `);
    }
}

const app = new DriverPage();
app.init();

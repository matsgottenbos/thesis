import * as Api from './api.js';
import * as Helper from './helper.js';
import * as Config from './config.js';
import * as ShiftHandler from './shiftHandler.js';

class DriverPage {
    async init() {
        const selectedRunName = Helper.getUrlParameters().get('run');
        const selectedScheduleName = Helper.getUrlParameters().get('schedule');
        const selectedDriverStr = Helper.getUrlParameters().get('driver');
        if (selectedRunName === null || selectedScheduleName === null || selectedDriverStr === null) {
            window.location = Config.homeUrl;
        }

        $('.backButton').click(() => {
            window.location = `${Config.homeUrl}schedule?run=${selectedRunName}&schedule=${selectedScheduleName}`;
        });

        const runData = await Api.getRunData(selectedRunName);
        const scheduleData = await Api.getScheduleData(selectedRunName, selectedScheduleName);

        const dataStartDate = new Date(runData.dataStartDate);

        const selectedDriverIndex = parseInt(selectedDriverStr) - 1;
        const driver = scheduleData.drivers[selectedDriverIndex];

        const name = Config.showRealDriverNames ? driver.realDriverName : driver.driverName;
        $('.pageTitle').html(name);

        // Driver info
        const driverDetails = {
            cost: '&euro; ' + Math.round(driver.stats.cost),
            '>costWithoutDelays': '&euro; ' + Math.round(driver.stats.rawCost),
            '>expectedDelayCost': '&euro; ' + Math.round(driver.stats.robustness),
            '>penaltyCost': '&euro; ' + Math.round(driver.stats.penalty),
            satisfaction: Math.round(driver.stats.driverSatisfaction * 100) + '%',
        };
        const driverDetailsHtmlParts = Helper.infoObjectToRows(driverDetails);
        $('.driverDetails .stats .rows').html(driverDetailsHtmlParts.join(''));

        if (driver.isInternal && !driver.isOptional) {
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
            contractTime: driver.isInternal && !driver.isOptional ? Helper.parseTimeSpan(driver.contractTime) : null,
            'internal / external': driver.isInternal ? 'Internal' : 'External',
            'national / international': driver.isInternational ? 'International' : 'National',
            'required / optional': driver.isOptional ? 'Optional' : 'Required',
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
        const shiftsHtmlParts = driver.shifts.map((shift, i) => ShiftHandler.getShiftHtml(shift, i, driver, dataStartDate));
        $('.driverPath').html(shiftsHtmlParts.join(''));
    }

    getShiftHtml(shiftIndex, shift, shiftHtmlParts, currentShiftDateStr, dataStartDate) {
        let shiftLengthStr = Helper.parseTimeSpan(shift.realMainShiftLength);
        if (shift.paidMainShiftLength >= shift.realMainShiftLength + 15) {
            shiftLengthStr += `, counted as minimum of ${Helper.parseTimeSpan(shift.paidMainShiftLength)}`;
        }

        const shiftInfo = {
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
            sharedCarTravelBeforeShift: this.getTimeAndDistanceStr(shift.sharedCarTravelTimeBefore, shift.sharedCarTravelDistanceBefore),
            ownCarTravelBeforeShift: this.getTimeAndDistanceStr(shift.ownCarTravelTimeBefore, shift.ownCarTravelDistanceBefore),
            sharedCarTravelAfterShift: this.getTimeAndDistanceStr(shift.sharedCarTravelTimeAfter, shift.sharedCarTravelDistanceAfter),
            ownCarTravelAfterShift: this.getTimeAndDistanceStr(shift.ownCarTravelTimeAfter, shift.ownCarTravelDistanceAfter),
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
                        <span class="cell startTime">Start time</span>
                        <span class="cell endTime">End time</span>
                        <span class="cell duration">Duration</span>
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

    getTimeAndDistanceStr(rawTimeSpan, distance) {
        if (rawTimeSpan == 0 && distance == 0) return '-';
        const timeStr = Helper.parseTimeSpan(rawTimeSpan);
        return `${timeStr}, ${distance} km`;
    }
}

const app = new DriverPage();
app.init();

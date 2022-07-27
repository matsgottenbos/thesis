import * as Api from './api.js';
import * as Helper from './helper.js';
import * as Config from './config.js';
import * as ShiftHandler from './shiftHandler.js';

class ShiftsPage {
    async init() {
        const selectedRunName = Helper.getUrlParameters().get('run');
        const selectedScheduleName = Helper.getUrlParameters().get('schedule');
        if (selectedRunName === null || selectedScheduleName === null) {
            window.location = Config.homeUrl;
        }

        $('.backButton').click(() => {
            window.location = `${Config.homeUrl}schedule?run=${selectedRunName}&schedule=${selectedScheduleName}`;
        });

        const runData = await Api.getRunData(selectedRunName);
        const scheduleData = await Api.getScheduleData(selectedRunName, selectedScheduleName);

        const dataStartDate = new Date(runData.dataStartDate);

        // Schedule shifts
        const scheduleShiftsWithDrivers = [];
        scheduleData.drivers.forEach(driver => {
            const driverShiftsWithDriver = driver.shifts.map(shift => ({ shift, driver }));
            scheduleShiftsWithDrivers.push(...driverShiftsWithDriver);
        });

        scheduleShiftsWithDrivers.sort((a, b) => {
            if (a.shift.mainShiftStartTime < b.shift.mainShiftStartTime) return -1;
            if (a.shift.mainShiftStartTime > b.shift.mainShiftStartTime) return 1;
            return 0;
        });

        const shiftsHtmlParts = scheduleShiftsWithDrivers.map((shiftWithDriver, i) => ShiftHandler.getShiftHtml(shiftWithDriver.shift, i, shiftWithDriver.driver, dataStartDate));
        $('.scheduleShifts').html(shiftsHtmlParts.join(''));
    }
}

const app = new ShiftsPage();
app.init();

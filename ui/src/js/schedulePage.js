import * as Config from './config.js';
import * as Api from './api.js';
import * as Helper from './helper.js';

class SchedulePage {
    async init() {
        const selectedRunName = Helper.getUrlParameters().get('run');
        const selectedScheduleName = Helper.getUrlParameters().get('schedule');
        if (selectedRunName === null || selectedScheduleName === null) {
            window.location = Config.homeUrl;
        }

        $('.backButton').click(() => {
            window.location = `${Config.homeUrl}run?run=${selectedRunName}`;
        });

        const runData = await Api.getRunData(selectedRunName);
        const scheduleData = await Api.getScheduleData(selectedRunName, selectedScheduleName);
        
        const dataStartDate = new Date(runData.dataStartDate);

        const scheduleInfo = {
            cost: '&euro; ' + Math.round(scheduleData.cost),
            '>rawCost': '&euro; ' + Math.round(scheduleData.rawCost),
            '>expectedDelaysCost': '&euro; ' + Math.round(scheduleData.robustness),
            '>penaltyCost': scheduleData.penalty > 0 ? '&euro; ' + Math.round(scheduleData.penalty) : null,
            satisfaction: Math.round(scheduleData.satisfaction * 100) + '%',
        };
        const scheduleInfoHtmlParts = Helper.infoObjectToRows(scheduleInfo);
        $('.scheduleInfoRows').html(scheduleInfoHtmlParts.join(''));

        const dayCount = Math.ceil(Config.originalTimeframeLength / (24 * 60));
        for (let i = 0; i < dayCount; i++) {
            const dateString = Helper.parseDateShort(i * 24 * 60, dataStartDate);
            $('.scheduleHeader').append(`<div class="dayHeader">${dateString}</div>`);
        }

        scheduleData.drivers.forEach((driver, i) => {
            if ((!driver.isInternal || driver.isOptional) && driver.shifts.length === 0) return;

            const name = Config.showRealDriverNames ? driver.realDriverName : driver.driverName;
            const satisfactionStr = driver.isInternal && !driver.isOptional ? ` (${Math.round(driver.stats.driverSatisfaction * 100)}%)` : '';
            $('.drivers').append(`<div class="driver" data-driver-index="${i}">${name}${satisfactionStr}</div>`);

            const activitiesHtmlParts = [];
            driver.shifts.forEach(shift => {
                shift.activityPath.forEach(item => {
                    const leftPercent = 100 * (item.startTime + Config.bufferBeforeTimeframe) / Config.timeframeLength;
                    const widthPercent = 100 * (item.endTime - item.startTime) / Config.timeframeLength;

                    let html = `<div class="pathItem ${item.type}" style="left: ${leftPercent}%; width: ${widthPercent}%"></div>`;
                    activitiesHtmlParts.push(html);
                });
            });

            $('.driverSchedules').append(`<div class="driverSchedule">${activitiesHtmlParts.join('')}</div>`);
        });

        $('.driver').click(function () {
            const driverIndex = parseInt($(this).attr('data-driver-index'));
            window.location = `../driver/?run=${selectedRunName}&schedule=${selectedScheduleName}&driver=${driverIndex + 1}`;
        });
    }
}

const app = new SchedulePage();
app.init();

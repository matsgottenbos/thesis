import * as Config from './config.js';
import * as Api from './api.js';
import * as Helper from './helper.js';

class VisualisePage {
    async init() {
        const selectedScheduleName = Helper.getUrlParameters().get('schedule');
        if (selectedScheduleName == null) {
            window.location = '../';
        }

        const data = await Api.getData(selectedScheduleName, '../');

        $('.backButton').click(() => {
            window.location = `../`;
        });

        const scheduleInfo = {
            cost: '&euro; ' + Math.round(data.cost),
            '>rawCost': '&euro; ' + Math.round(data.rawCost),
            '>expectedDelaysCost': '&euro; ' + Math.round(data.robustness),
            '>penaltyCost': data.penalty > 0 ? '&euro; ' + Math.round(data.penalty) : null,
            satisfaction: Math.round(data.satisfaction * 100) + '%',
        };
        const scheduleInfoHtmlParts = Helper.infoObjectToRows(scheduleInfo);
        $('.scheduleInfoRows').html(scheduleInfoHtmlParts.join(''));

        const dayCount = Math.ceil(Config.originalTimeframeLength / (24 * 60));
        for (let i = 0; i < dayCount; i++) {
            const dateString = Helper.parseDateShort(i * 24 * 60);
            $('.scheduleHeader').append(`<div class="dayHeader">${dateString}</div>`);
        }

        data.drivers.forEach((driver, i) => {
            if (!driver.isInternal && driver.shifts.length === 0) return;

            const name = Config.showRealDriverNames ? driver.realDriverName : driver.driverName;
            const satisfactionStr = driver.isInternal ? ` (${Math.round(driver.stats.driverSatisfaction * 100)}%)` : '';
            $('.drivers').append(`<div class="driver" data-driver-index="${i}">${name}${satisfactionStr}</div>`);

            const tripsHtmlParts = [];
            driver.shifts.forEach(shift => {
                shift.tripPath.forEach(item => {
                    const leftPercent = 100 * (item.startTime + Config.bufferBeforeTimeframe) / Config.timeframeLength;
                    const widthPercent = 100 * (item.endTime - item.startTime) / Config.timeframeLength;

                    let html = `<div class="pathItem ${item.type}" style="left: ${leftPercent}%; width: ${widthPercent}%"></div>`;
                    tripsHtmlParts.push(html);
                });
            });

            $('.driverSchedules').append(`<div class="driverSchedule">${tripsHtmlParts.join('')}</div>`);
        });

        $('.driver').click(function () {
            const driverIndex = parseInt($(this).attr('data-driver-index'));
            window.location = `../driver/?schedule=${selectedScheduleName}&driver=${driverIndex + 1}`;
        });
    }
}

const app = new VisualisePage();
app.init();

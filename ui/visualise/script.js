import * as Config from '../shared/config.js';
import * as Api from '../shared/api.js';
import * as Helper from '../shared/helper.js';


class VisualiseApp {
    async init() {
        const data = await Api.getData();

        const dayCount = Math.ceil(Config.originalTimeframeLength / (24 * 60));
        for (let i = 0; i < dayCount; i++) {
            const dateString = Helper.parseDateShort(i * 24 * 60);
            $('.scheduleHeader').append(`<div class="dayHeader">${dateString}</div>`);
        }

        data.drivers.forEach(driver => {
            if (driver.driverPath.length === 0) return;

            const name = Config.showRealDriverNames ? driver.realDriverName : driver.driverName;
            const satisfactionStr = driver.isInternal ? ` (${Math.round(driver.stats.driverSatisfaction * 100)}%)` : '';
            $('.drivers').append(`<div class="driver">${name}${satisfactionStr}</div>`);

            const tripsHtml = driver.driverPath.map(item => {
                const leftPercent = 100 * (item.startTime + Config.bufferBeforeTimeframe) / Config.timeframeLength;
                const widthPercent = 100 * (item.endTime - item.startTime) / Config.timeframeLength;

                let html = `<div class="pathItem ${item.type}" style="left: ${leftPercent}%; width: ${widthPercent}%"></div>`;

                return html;
            });

            $('.driverSchedules').append(`<div class="driverSchedule">${tripsHtml.join('')}</div>`);
        });
    }
}

const app = new VisualiseApp();
app.init();

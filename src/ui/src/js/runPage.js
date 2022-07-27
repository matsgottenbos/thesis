import * as Api from './api.js';
import * as Helper from './helper.js';
import * as Config from './config.js';

class RunPage {
    async init() {
        const selectedRunName = Helper.getUrlParameters().get('run');
        if (selectedRunName === null) {
            window.location = Config.homeUrl;
        }

        $('.backButton').click(() => {
            window.location = Config.homeUrl;
        });

        const runData = await Api.getRunData(selectedRunName);

        const schedulesHtmlParts = runData.schedules.map(schedule => `
            <div class="scheduleTile tile" data-schedule-name="${schedule.fileName}">
                <div class="cost">&euro; ${Math.round(schedule.cost)}</div>
                <div class="satisfaction">${Math.round(schedule.satisfaction * 100)}%</div>
            </div>
        `);
        const schedulesHtml = schedulesHtmlParts.join('');
        $('.scheduleOptions').html(schedulesHtml);

        $('.scheduleTile').click(function() {
            const name = $(this).attr('data-schedule-name');
            window.location = `${Config.homeUrl}schedule/?run=${selectedRunName}&schedule=${name}`;
        });
    }
}

const app = new RunPage();
app.init();

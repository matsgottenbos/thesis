import * as Config from './config.js';
import * as Api from './api.js';

class VisualisePage {
    async init() {
        const schedules = await Promise.all(Config.scheduleNames.map(async scheduleName => {
            const scheduleData = await Api.getData(scheduleName);

            return {
                name: scheduleName,
                cost: scheduleData.cost,
                satisfaction: scheduleData.satisfaction,
            }
        }));

        const schedulesHtmlParts = schedules.map(schedule => `
            <div class="scheduleTile" data-name="${schedule.name}">
                <div class="cost">&euro; ${Math.round(schedule.cost)}</div>
                <div class="satisfaction">${Math.round(schedule.satisfaction * 100)}%</div>
            </div>
        `);
        const schedulesHtml = schedulesHtmlParts.join('');
        $('.scheduleOptions').html(schedulesHtml);

        $('.scheduleTile').click(function() {
            const name = $(this).attr('data-name');
            window.location = `./schedule/?schedule=${name}`;
        });
    }
}

const app = new VisualisePage();
app.init();

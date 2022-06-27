import * as Config from './config.js';
import * as Api from './api.js';
import * as Helper from './helper.js';

class VisualisePage {
    async init() {
        const scheduleNames = [
            '67k-41p',
            '68k-43p',
            '69k-48p',
            '72k-51p',
            '77k-56p',
            '78k-57p',
        ];

        const schedules = await Promise.all(scheduleNames.map(async scheduleName => {
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

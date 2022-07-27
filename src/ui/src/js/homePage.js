import * as Api from './api.js';
import * as Config from './config.js';

class RunPage {
    async init() {
        const runListData = await Api.getRunListData();

        const runListHtmlParts = runListData.runsByStartDate.map(dateRunList => {
            const dateRunListHtmlParts = dateRunList.runs.map(run => `
                <div class="runTile tile" data-run-name="${run.folderName}">
                    <div>Completed ${run.runCompletionDate}</div>
                    <div>${this.largeNumToString(run.iterationCount)} iterations</div>
                    <div>${run.schedules.length} ${run.schedules.length === 1 ? 'schedule' : 'schedules'}</div>
                </div>
            `);
            const dateRunListHtml = dateRunListHtmlParts.join('');

            return `
                <div class="dateRunList">
                    <h2>${dateRunList.dataStartDate} - ${dateRunList.dataEndDate}</h2>
                    <div class="runTileContainer">${dateRunListHtml}</div>
                </div>
            `;
        });
        const runListHtml = runListHtmlParts.length > 0 ? runListHtmlParts.join('') : '<div class="pageInfo">No runs found</div>';
        $('.runList').html(runListHtml);

        $('.runTile').click(function() {
            const runName = $(this).attr('data-run-name');
            window.location = `${Config.homeUrl}run/?run=${runName}`;
        });
    }

    largeNumToString(num, decimalCount = 0) {
        if (num > 1000000000) {
            return (num / 1000000000).toFixed(decimalCount) + ' billion';
        }
        if (num > 1000000) {
            return (num / 1000000).toFixed(decimalCount) + ' million';
        }
        if (num > 1000) {
            return (num / 1000).toFixed(decimalCount) + '.000';
        }
        return num;
    }
}

const app = new RunPage();
app.init();

import * as Api from '../shared/api.js';
import * as Helper from '../shared/helper.js';

class VisualiseDriverApp {
    data = null;

    async init() {
        if (!window.location.hash) {
            window.location.hash = '#1';
        }

        this.data = await Api.getData();

        addEventListener('hashchange', () => this.updateContent());
        this.updateContent();
    }

    updateContent() {
        const selectedDriverIndex = parseInt(window.location.hash.substring(1)) - 1;
        const driver = this.data.drivers[selectedDriverIndex];

        const name = this.showRealDriverNames ? driver.realDriverName : driver.driverName;
        $('.driverName').html(name);

        const shiftsHtmlParts = [];
        let shiftHtmlParts = [];
        let currentShiftDateStr = null;
        driver.driverPath.forEach(item => {
            if (currentShiftDateStr === null) currentShiftDateStr = Helper.parseDate(item.startTime);
            const startTimeStr = Helper.parseTime(item.startTime);
            const endTimeStr = Helper.parseTime(item.endTime);
            const durationStr = Helper.parseTimeDiff(item.startTime, item.endTime);

            // Deal with legacy property names
            const startStationName = item.startStationName || item.startStationCode;
            const endStationName = item.endStationName || item.endStationCode;

            if (item.type === 'trip') {
                shiftHtmlParts.push(`
                    <div class="row pathItem ${item.type}">
                        <span class="cell startTime">${startTimeStr}</span>
                        <span class="cell endTime">${endTimeStr}</span>
                        <span class="cell duration">${durationStr}</span>
                        <span class="cell duty">${item.dutyName}</span>
                        <span class="cell activity">${item.activityName}</span>
                        <span class="cell fromLocation">${startStationName}</span>
                        <span class="cell toLocation">${endStationName}</span>
                    </div>
                `);
            } else {
                let description;
                if (item.type === 'travelBetween') description = 'Travel between activities';
                if (item.type === 'wait') description = 'Waiting';
                else if (item.type === 'travelBefore') description = 'Travel from home';
                else if (item.type === 'travelAfter') description = 'Travel to home';
                else if (item.type === 'travelBeforeHotel') description = 'Travel to hotel';
                else if (item.type === 'travelAfterHotel') description = 'Travel from hotel';
                else if (item.type === 'rest') description = 'Rest';
                else if (item.type === 'hotel') description = 'Hotel stay';

                shiftHtmlParts.push(`
                    <div class="row pathItem ${item.type}">
                        <span class="cell startTime">${startTimeStr}</span>
                        <span class="cell endTime">${endTimeStr}</span>
                        <span class="cell duration">${durationStr}</span>
                        <span class="cell name">${description}</span>
                    </div>
                `);
            }

            if (item.type === 'rest' || item.type === 'hotel') {
                shiftsHtmlParts.push(`
                    <div class="shift">
                        <div class="shiftHeader">Shift ${shiftsHtmlParts.length + 1}: ${currentShiftDateStr}</div>
                        <div class="row shiftPathHeader">
                            <span class="cell startTime">Start time</span>
                            <span class="cell endTime">End time</span>
                            <span class="cell duration">Duration</span>
                            <span class="cell duty">Duty</span>
                            <span class="cell activity">Activity</span>
                            <span class="cell fromLocation">From location</span>
                            <span class="cell toLocation">To location</span>
                        </div>
                        <div class="shiftPath">${shiftHtmlParts.join('')}</div>
                    </div>
                `);

                shiftHtmlParts = [];
                currentShiftDateStr = null;
            }
        });

        shiftsHtmlParts.push(`
            <div class="shift">
                <div class="shiftHeader">Shift ${shiftsHtmlParts.length + 1}: ${currentShiftDateStr}</div>
                <div class="row shiftPathHeader">
                    <span class="cell startTime">Start time</span>
                    <span class="cell endTime">End time</span>
                    <span class="cell duty">Duty</span>
                    <span class="cell activity">Activity</span>
                    <span class="cell fromLocation">From location</span>
                    <span class="cell toLocation">To location</span>
                </div>
                <div class="shiftPath">${shiftHtmlParts.join('')}</div>
            </div>
        `);

        $('.driverPath').html(shiftsHtmlParts.join(''));
    }
}

const app = new VisualiseDriverApp();
app.init();

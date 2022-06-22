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

        // Driver info
        const driverDetails = {
            cost: Math.round(driver.stats.cost),
            rawCost: Math.round(driver.stats.rawCost),
            robustness: Math.round(driver.stats.robustness),
            satisfaction: Math.round(driver.stats.driverSatisfaction * 100) + '%',
        };

        const driverStatsHtmlParts = Object.keys(driverDetails).map(key => `<div class="row"><span class="label">${this.camelCaseToWords(key)}</span><span class="value">${driverDetails[key]}</span></div>`);
        $('.driverDetails .stats .rows').html(driverStatsHtmlParts.join(''));

        const driverSatisfactionCriteriaHtmlParts = Object.keys(driver.stats.driverSatisfactionCriteria).map(key => {
            const value = Math.round(driver.stats.driverSatisfactionCriteria[key] * 100);
            return `<div class="row"><span class="label">${key}</span><span class="value">${value}%</span></div>`;
        });
        $('.driverDetails .satisfactionCriteria .rows').html(driverSatisfactionCriteriaHtmlParts.join(''));

        const driverInfo = {
            contractTime: driver.contractTime,
            ...driver.info,
        }

        const driverInfoHtmlParts = Object.keys(driverInfo).map(key => `<div class="row"><span class="label">${this.camelCaseToWords(key)}</span><span class="value">${driverInfo[key]}</span></div>`);
        $('.driverDetails .info .rows').html(driverInfoHtmlParts.join(''));
        

        // Driver path
        const shiftsHtmlParts = [];
        let shiftHtmlParts = [];
        let currentShiftDateStr = null;
        driver.driverPath.forEach(item => {
            if (currentShiftDateStr === null) currentShiftDateStr = Helper.parseDate(item.startTime);
            const startTimeStr = Helper.parseTime(item.startTime);
            const endTimeStr = Helper.parseTime(item.endTime);
            const durationStr = Helper.parseTimeDiff(item.startTime, item.endTime);

            // Deal with legacy property names
            const startStationNameStr = item.startStationName || item.startStationCode;
            const endStationNameStr = item.endStationName || item.endStationCode;

            if (item.type === 'trip') {
                shiftHtmlParts.push(`
                    <div class="row pathItem ${item.type}">
                        <span class="cell id">${item.tripIndex}</span>
                        <span class="cell startTime">${startTimeStr}</span>
                        <span class="cell endTime">${endTimeStr}</span>
                        <span class="cell duration">${durationStr}</span>
                        <span class="cell duty">${item.dutyName}</span>
                        <span class="cell activity">${item.activityName}</span>
                        <span class="cell fromLocation">${startStationNameStr}</span>
                        <span class="cell toLocation">${endStationNameStr}</span>
                    </div>
                `);
            } else {
                let description;
                if (item.type === 'travelBetween') description = 'Travel between activities';
                else if (item.type === 'wait') description = 'Waiting';
                else if (item.type === 'travelBefore') description = 'Travel from home';
                else if (item.type === 'travelAfter') description = 'Travel to home';
                else if (item.type === 'travelBeforeHotel') description = 'Travel to hotel';
                else if (item.type === 'travelAfterHotel') description = 'Travel from hotel';
                else if (item.type === 'rest') description = 'Rest';
                else if (item.type === 'hotel') description = 'Hotel stay';
                else if (item.type === 'overlapError') description = 'Overlap error';

                shiftHtmlParts.push(`
                    <div class="row pathItem ${item.type}">
                        <span class="cell id"></span>
                        <span class="cell startTime">${startTimeStr}</span>
                        <span class="cell endTime">${endTimeStr}</span>
                        <span class="cell duration">${durationStr}</span>
                        <span class="cell name">${description}</span>
                    </div>
                `);
            }

            if (item.type === 'rest' || item.type === 'hotel') {
                shiftsHtmlParts.push(this.getShiftHeader(shiftsHtmlParts.length, shiftHtmlParts, currentShiftDateStr));
                shiftHtmlParts = [];
                currentShiftDateStr = null;
            }
        });

        shiftsHtmlParts.push(this.getShiftHeader(shiftsHtmlParts.length, shiftHtmlParts, currentShiftDateStr));

        $('.driverPath').html(shiftsHtmlParts.join(''));
    }

    getShiftHeader(shiftIndex, shiftHtmlParts, currentShiftDateStr) {
        return (`
            <div class="shift">
                <div class="shiftHeader">Shift ${shiftIndex + 1}: ${currentShiftDateStr}</div>
                <div class="row shiftPathHeader">
                    <span class="cell id">Trip ID</span>
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
    }

    camelCaseToWords(camelCaseStr) {
        let result = camelCaseStr.replace(/([A-Z])/g, " $1");
        result = result.toLowerCase();
        result = result.charAt(0).toUpperCase() + result.slice(1);
        return result;
    }
}

const app = new VisualiseDriverApp();
app.init();

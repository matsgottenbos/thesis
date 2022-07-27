import * as Config from './config.js';

async function checkForErrors (response) {
    if (response.status === 200) return;

    const json = await response.json();
    if (json?.error) throw Error(`API call 'getData' returned error ${response.status}: '${json.error}'`);
    else throw Error(`API call 'getData' returned error ${response.status}`);
}

export async function getRunListData() {
    return getJsonData(`${Config.homeUrl}${Config.outputRootPath}/runList.json`);
}

export async function getRunData(selectedRunName) {
    const runListData = await getRunListData();
    return findRun(runListData.runsByStartDate, selectedRunName);
}

export async function getScheduleData(selectedRunName, selectedScheduleName) {
    return getJsonData(`${Config.homeUrl}${Config.outputRootPath}${selectedRunName}/${selectedScheduleName}.json`);
}

async function getJsonData(filePath) {
    const response = await fetch(filePath, {
        method: 'GET',
    });
    checkForErrors(response);
    return response.json();
}

function findRun(runsByStartDate, selectedRunName) {
    let foundRun = null;
    runsByStartDate.forEach(dateRunList => {
        dateRunList.runs.forEach(run => {
            if (run.folderName === selectedRunName) {
                foundRun = run;
            }
        });
    });
    return foundRun;
}
import * as Config from './config.js';

export async function checkForErrors (response) {
    if (response.status === 200) return;

    const json = await response.json();
    if (json?.error) throw Error(`API call 'getData' returned error ${response.status}: '${json.error}'`);
    else throw Error(`API call 'getData' returned error ${response.status}`);
}

export async function getData(selectedScheduleName, uiRootPath = './') {
    const response = await fetch(`${uiRootPath}${Config.outputRootPath}${Config.selectedOutputFolder}/${selectedScheduleName}.json`, {
        method: 'GET',
    });
    this.checkForErrors(response);

    return await response.json();
}

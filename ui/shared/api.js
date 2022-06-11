import * as Config from './config.js';

export async function checkForErrors (response) {
    if (response.status === 200) return;

    const json = await response.json();
    if (json?.error) throw Error(`API call 'getData' returned error ${response.status}: '${json.error}'`);
    else throw Error(`API call 'getData' returned error ${response.status}`);
}

export async function getData() {
    const response = await fetch(`../../output/${Config.jsonFile}.json`, {
        method: 'GET',
    });
    this.checkForErrors(response);

    return await response.json();
}

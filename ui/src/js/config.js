export const selectedOutputFolder = '2022-07-11-05-13';
export const scheduleNames = [
    '39k-39p',
    '40k-50p',
    '40k-55p',
    '41k-63p',
    '42k-65p',
    '43k-67p',
    '46k-68p',
];
export const outputRootPath = './output/';
export const showRealDriverNames = false;
export const shouldShowActivityIds = true;
export const startDate = new Date(2022, 6, 11); // NB: months are 0-based, days are 1-based; TODO: get this from JSON
export const originalTimeframeLength = 7 * 24 * 60;
export const bufferBeforeTimeframe = 2 * 60;
export const bufferAfterTimeframe = 8 * 60;
export const timeframeLength = originalTimeframeLength + bufferBeforeTimeframe + bufferAfterTimeframe;

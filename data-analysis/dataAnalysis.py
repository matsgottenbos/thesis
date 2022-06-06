import json
from math import floor
import matplotlib.pyplot as plt
import numpy as np
import scipy.stats as stats
from matplotlib.ticker import PercentFormatter

### Helpers

def plotHistogram(values, binMin=-600, binMax=600, binSize=30, ylim=None, color=(0, 0.5, 1, 1), alpha=1, histtype='bar'):
    count = len(values)
    bins=np.arange(binMin, binMax + binSize, binSize)
    weights = np.ones(count) / count
    plt.hist(values, bins=bins, color=color, weights=weights, histtype=histtype, alpha=alpha)
    plt.xlim([binMin, binMax])
    plt.gca().yaxis.set_major_formatter(PercentFormatter(1)) # Show percentagess
    if ylim != None: plt.ylim(ylim)

def plotDistribution(cdfFunc, binMin=-600, binMax=600, binSize=30, ylim=None, color=(0, 0.5, 1, 1), lineWidth=1, precision=10):
    binCount = int((binMax - binMin) / binSize) * precision

    cdfPoints = np.linspace(binMin, binMax, binCount + 2)
    scaledCdf = cdfFunc(cdfPoints)
    cdf = [precision * cdfVal for cdfVal in scaledCdf]

    groupedCdf = [cdf[i + 1] - cdf[i] for i in range(len(cdf) - 1)]
    
    x2 = np.linspace(binMin, binMax, binCount + 1)
    plt.plot(x2, groupedCdf, color=color, linewidth=lineWidth)
    if ylim != None: plt.ylim(ylim)

def beforePlot(xLabel=None, yLabel=None):
    fig = plt.figure(figsize=(5,5))
    ax = plt.subplot()
    if (xLabel != None): plt.xlabel(xLabel)
    if (yLabel != None): plt.ylabel(yLabel)
    return fig, ax

def afterPlot(plotName):
    # plt.show()
    filePath = './data-analysis/plots/{0}.png'.format(plotName)
    plt.savefig(filePath, dpi=300, bbox_inches='tight', pad_inches=0.1)
    plt.close()


### Output

# Basic info
def printBasicInfo(allDelays, allPositiveDelays):
    print('Number of activities:', len(allDelays))
    print('Number of delayed activities:', len(allPositiveDelays))
    print('Percentage of activities delayed:', str(100 * len(allPositiveDelays) / len(allDelays)) + "%")

# All delays histogram
def plotAllDelays(allDelays):
    beforePlot(xLabel='Delay amount (minutes)', yLabel='Share of total')
    plotHistogram(allDelays, binMin=-300, binMax=300, binSize=10)
    afterPlot(plotName='delays')

# Positive delays histogram
def plotAllPositiveDelays(allPositiveDelays):
    beforePlot(xLabel='Delay amount (minutes)', yLabel='Share of total')
    plotHistogram(allPositiveDelays, binMin=0, binMax=300, binSize=5)
    plt.ylim([0, 0.19])
    afterPlot(plotName='delays-positive')

# Driving vs non-driving info
def printDrivingNonDrivingInfo(allDelaysDriving, allDelaysNonDriving, allPositiveDelaysDriving, allPositiveDelaysNonDriving):
    print('Number of driving activities:', len(allDelaysDriving))
    print('Number of non-driving activities:', len(allDelaysNonDriving))
    print('Number of delayed driving activities:', len(allPositiveDelaysDriving))
    print('Number of delayed non-driving activities:', len(allPositiveDelaysNonDriving))
    print('Percentage of driving activities delayed:', str(100 * len(allPositiveDelaysDriving) / len(allDelaysDriving)) + "%")
    print('Percentage of non-driving activities delayed:', str(100 * len(allPositiveDelaysNonDriving) / len(allDelaysNonDriving)) + "%")

# Delays driving vs non-driving
def plotDelaysDrivingNonDriving(allDelaysDriving, allDelaysNonDriving):
    beforePlot(xLabel='Delay amount (minutes)', yLabel='Share of total')
    plotHistogram(allDelaysDriving, binMin=-300, binMax=300, binSize=10, color=(0, 0.5, 1, 1), alpha=0.6)
    plotHistogram(allDelaysNonDriving, binMin=-300, binMax=300, binSize=10, color=(1, 0.5, 0, 1), alpha=0.6)
    afterPlot(plotName='delays-driving-nondriving')

# Positive driving vs non-driving
def plotPositiveDelaysDrivingNonDriving(allPositiveDelaysDriving, allPositiveDelaysNonDriving):
    beforePlot(xLabel='Delay amount (minutes)', yLabel='Share of total')
    plotHistogram(allPositiveDelaysDriving, binMin=0, binMax=300, binSize=5, color=(0, 0.5, 1, 1), alpha=0.6)
    plotHistogram(allPositiveDelaysNonDriving, binMin=0, binMax=300, binSize=5, color=(1, 0.5, 0, 1), alpha=0.6)
    afterPlot(plotName='delays-positive-driving-nondriving')

# Positive delays gamma distribution
def fitGammaDistributionToAllPositiveDelays(allPositiveDelays):
    allPositiveDelaysCapped = [delay for delay in allPositiveDelays if delay < 600]
    a, _, scale = stats.gamma.fit(allPositiveDelaysCapped, loc=0, floc=0)
    print('All positive delays gamma distribution, alpha parameter:', a)
    print('All positive delays gamma distribution, beta parameter:', 1 / scale)

    # Print histogram comparison
    beforePlot(xLabel='Delay amount (minutes)', yLabel='Share of total')
    plotHistogram(allPositiveDelays, binMin=0, binMax=300, binSize=5, ylim=None)
    plotDistribution(lambda x: stats.gamma.cdf(x, a, 0, scale), binMin=0, binMax=300, binSize=5, ylim=None, color=(1, 0.5, 0, 1), lineWidth=3)
    plt.ylim([0, 0.19])
    afterPlot(plotName='gamma-fit-histogram')

    # Print probability plot
    _, ax = beforePlot()
    _, (_, _, r) = stats.probplot(allPositiveDelaysCapped, dist=stats.gamma, sparams=(a, 0, scale), plot=plt)
    print('Coefficient of determination:', r * r)
    plt.title('')
    plt.xlabel('Distribution quantiles')
    plt.ylabel('Data quantiles')
    ax.get_lines()[0].set_markerfacecolor((0, 0.5, 1, 0.2))
    ax.get_lines()[0].set_markeredgewidth(0)
    ax.get_lines()[1].set_color((1, 0.5, 0, 1))
    ax.get_lines()[1].set_linewidth(3)
    afterPlot(plotName='gamma-fit-probplot')

# Determine function of mean delay by duration
def fitMeanDelayFunction(allPositiveDelaysFrequentDurations, allPositiveDelaysFrequent):
    beforePlot(xLabel='Duration (minutes)', yLabel='Delay amount (minutes)')
    muXs = allPositiveDelaysFrequentDurations
    muXsModel = sorted(muXs)
    muYs = allPositiveDelaysFrequent
    muCoef = np.polyfit(muXs, muYs, 2)
    muFunc = lambda x: muCoef[0] * x * x + muCoef[1] * x + muCoef[2]
    print('Mean delay by duration: %sx^2 + %sx + %s' % (muCoef[0], muCoef[1], muCoef[2]))
    muYsModel = [muFunc(x) for x in muXsModel]
    plt.plot(muXs, muYs, 'o', color=(0, 0.5, 1, 0.1))
    plt.plot(muXsModel, muYsModel, color=(1, 0.5, 0, 1), linewidth=3)
    plt.xlim([0, 600])
    plt.ylim([0, 1000])
    afterPlot(plotName='delays-duration-mean')

# Determine function of delay standard deviation by duration
def showStdScatterPlot(allPositiveDelaysByDuration, durationIndexSize):
    beforePlot(xLabel='Duration (minutes)', yLabel='Delay standard deviation (minutes)')
    stdDelayByDuration = []
    for durationIndex in range(len(allPositiveDelaysByDuration)):
        durationDelays = allPositiveDelaysByDuration[durationIndex]
        if (len(durationDelays) < 5): continue
        _, std = stats.norm.fit(durationDelays)
        stdDelayByDuration.append(std)
    stdYs = stdDelayByDuration[0:18]
    stdXs = range(0, len(stdYs) * durationIndexSize, durationIndexSize)
    plt.plot(stdXs, stdYs, 'o', color=(0, 0.5, 1, 1))
    afterPlot(plotName='delays-duration-std')



### Run

def run(durationIndexSize):
    # Read data
    with open('./output/delays.json', 'r') as readFile:
        data = json.load(readFile)
    activities = data['activities']

    # Process data
    allDelays = []
    allDelaysDriving = []
    allDelaysNonDriving = []
    allPositiveDelays = []
    allPositiveDelaysDriving = []
    allPositiveDelaysNonDriving = []
    allPositiveDelaysFrequent = []
    allPositiveDelaysFrequentDurations = []
    allPositiveDelaysByDuration = [] # durations rounded down to nearest `durationIndexSize`
    delayedCountByDuration = []
    for activity in activities:
        plannedDuration = activity['plannedDuration']
        delays = activity['durationDelays']
        allDelays.extend(delays)
        positiveDelays = [delay for delay in delays if delay > 0]
        allPositiveDelays.extend(positiveDelays)

        if activity['description'] == 'Drive train':
            allDelaysDriving.extend(delays)
            allPositiveDelaysDriving.extend(positiveDelays)
        else:
            allDelaysNonDriving.extend(delays)
            allPositiveDelaysNonDriving.extend(positiveDelays)

        durationIndex = floor(plannedDuration / durationIndexSize)
        while len(allPositiveDelaysByDuration) < durationIndex + 1:
            allPositiveDelaysByDuration.append([])
            delayedCountByDuration.append(0)
        allPositiveDelaysByDuration[durationIndex].extend(positiveDelays)
        delayedCountByDuration[durationIndex] += len(positiveDelays)

        if activity['occurrenceCount'] < 10:
            continue

        allPositiveDelaysFrequent.extend(positiveDelays)
        for _ in range(len(positiveDelays)):
            allPositiveDelaysFrequentDurations.append(plannedDuration)

    # Perform output
    printBasicInfo(allDelays, allPositiveDelays)
    plotAllDelays(allDelays)
    plotAllPositiveDelays(allPositiveDelays)
    printDrivingNonDrivingInfo(allDelaysDriving, allDelaysNonDriving, allPositiveDelaysDriving, allPositiveDelaysNonDriving)
    plotDelaysDrivingNonDriving(allDelaysDriving, allDelaysNonDriving)
    plotPositiveDelaysDrivingNonDriving(allPositiveDelaysDriving, allPositiveDelaysNonDriving)
    fitGammaDistributionToAllPositiveDelays(allPositiveDelays)
    fitMeanDelayFunction(allPositiveDelaysFrequentDurations, allPositiveDelaysFrequent)
    showStdScatterPlot(allPositiveDelaysByDuration, durationIndexSize)

run(durationIndexSize=30)

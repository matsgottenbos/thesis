import json
from math import floor
import matplotlib.pyplot as plt
import numpy as np
import scipy.stats as stats

def plotHistogram(values, binMin=-600, binMax=600, binSize=30, ylim=[0, 1], color=(0, 0, 1, 1), alpha=1, histtype='bar'):
    count = len(values)
    bins=np.arange(binMin, binMax + binSize, binSize)
    weights = np.ones(count) / count
    plt.hist(values, bins=bins, color=color, weights=weights, histtype=histtype, alpha=alpha)
    plt.xlim([binMin, binMax])
    if ylim != None: plt.ylim(ylim)

def plotLineHistogram(values, binMin=-600, binMax=600, binSize=30, ylim=[0, 1], color=(0, 0, 1, 1)):
    binCount = int((binMax - binMin) / binSize)

    roundedValues = [round(value / 30) * 30 for value in values]
    count = len(roundedValues)
    originalUniqueValues, originalCounts = np.unique(roundedValues, return_counts=True)
    originalCountFractions = originalCounts / count

    uniqueValues = np.linspace(binMin, binMax, binCount + 1)
    countFractions = []
    for value in uniqueValues:
        if value in originalUniqueValues:
            # In data, get actual count fraction
            labelIndex = list(originalUniqueValues).index(value)
            labelCountFraction = originalCountFractions[labelIndex]
            countFractions.append(labelCountFraction)
        else:
            # Not in data, set to 0
            countFractions.append(0)

    plt.plot(uniqueValues, countFractions, color=color)
    plt.xlim([binMin, binMax])
    if ylim != None: plt.ylim(ylim)

def plotDistribution(cdfFunc, binMin=-600, binMax=600, binSize=30, ylim=[0, 1], color=(0, 0, 1, 1), precision=10):
    binCount = int((binMax - binMin) / binSize) * precision

    cdfPoints = np.linspace(binMin, binMax, binCount + 2)
    scaledCdf = cdfFunc(cdfPoints)
    cdf = [precision * cdfVal for cdfVal in scaledCdf]

    groupedCdf = [cdf[i + 1] - cdf[i] for i in range(len(cdf) - 1)]
    
    x2 = np.linspace(binMin, binMax, binCount + 1)
    plt.plot(x2, groupedCdf, color=color)
    if ylim != None: plt.ylim(ylim)


with open('./output/delays.json', 'r') as readFile:
    data = json.load(readFile)

activities = data['activities']

fig = plt.figure(figsize=(9,9))
ax = plt.subplot()

i = 0
allDelays = []
allRelativeDelays = []
allPositiveDelays = []
allPositiveDelaysByDuration = [] # durations rounded down to nearest 30
muPlusStd = []
delayScores = []
delayInfos = []
delayedCount = 0
countByDuration = []
delayedCountByDuration = []
for activity in activities:
    plannedDuration = activity['plannedDuration']
    delays = activity['durationDelays']
    allDelays.extend(delays)
    actualDurations = [plannedDuration + delay for delay in delays]
    relativeDelays = [delay / max(15, plannedDuration) for delay in delays]
    allRelativeDelays.extend(relativeDelays)
    clippedDelays = [max(delay, 0) for delay in delays]
    positiveDelays = [delay for delay in delays if delay > 0]
    allPositiveDelays.extend(positiveDelays)
    delayedCount += len(positiveDelays)

    durationIndex = floor(plannedDuration / 30)
    while len(allPositiveDelaysByDuration) < durationIndex + 1:
        allPositiveDelaysByDuration.append([])
        countByDuration.append(0)
        delayedCountByDuration.append(0)
    allPositiveDelaysByDuration[durationIndex].extend(positiveDelays)
    countByDuration[durationIndex] += len(delays)
    delayedCountByDuration[durationIndex] += len(positiveDelays)

    if activity['occurrenceCount'] < 10:
        continue

    hasNonZeroValue = False
    for delay in delays:
        if delay != 0: hasNonZeroValue = True

    if not hasNonZeroValue:
        continue

    mu, std = stats.norm.fit(delays)
    muPlusStd.append(mu + std)

    count = len(delays)
    noDelayCount = 0
    subHourDelayCount = 0
    superHourDelayCount = 0
    for delay in delays:
        relativeDelay = delay / max(15, plannedDuration)
        if delay <= 15: noDelayCount += 1
        elif delay < 60: subHourDelayCount += 1
        else: superHourDelayCount += 1

    noDelayPercent = round(100 * noDelayCount / count)
    subHourDelayPercent = round(100 * subHourDelayCount / count)
    superHourDelayPercent = round(100 * superHourDelayCount / count)

    delayScore = subHourDelayPercent + 2 * superHourDelayPercent
    delayScores.append(delayScore)
    delayInfos.append((delayScore, noDelayPercent, subHourDelayPercent, superHourDelayPercent))
    
    # print(delayScore, noDelayPercent, subHourDelayPercent, superHourDelayPercent)


    # plotHistogram(values)
    # plotLineHistogram(values)
    # plotFittedNormalDistribution(mu, std)
    
    # print(relativeDelays)
    # plotHistogram(relativeDelays, binMin=-2, binMax=2, binSize=0.05)

    # print(plannedDuration)
    # plotHistogram(actualDurations, binMin=0, binMax=600, binSize=15)

    # plt.plot([plannedDuration, plannedDuration], [0, count], color='k')

    # plotHistogram(clippedDelays, binMin=0, binMax=200, binSize=5)

    # loc, scale = expon.fit(clippedDelays)
    # print(loc, scale)
    # distX = np.linspace(0, 200, 1000)
    # distY = expon.pdf(distX, loc, scale)
    # plt.plot(distX, distY, color='red')

    # plt.xlim([-5, 200])
    # plt.ylim([0, count])

    # plt.show()

    # i += 1

# print(i)

delayedFraction = delayedCount / len(allDelays)
print(delayedCount, len(allDelays), delayedFraction)

allDelayedPercentages = [100 - delayInfo[1] for delayInfo in delayInfos]
plotHistogram(allDelayedPercentages, binMin=0, binMax=100, binSize=1)

# allPositiveDelays = [delay for delay in allPositiveDelays if delay < 600]

# plotHistogram(allDelays, binMin=-120, binMax=120, binSize=16)
# plotHistogram(allDelays, binMin=0, binMax=1200, binSize=20)
# plotHistogram(allPositiveDelays, binMin=0, binMax=600, binSize=2, ylim=None, histtype='bar')

# delayedFractionByDuration = []
# meanDelayByDuration = []
# stdDelayByDuration = []
# for durationIndex in range(len(allPositiveDelaysByDuration)):
#     durationDelays = allPositiveDelaysByDuration[durationIndex]
#     durationCount = countByDuration[durationIndex]
#     durationDelayedCount = delayedCountByDuration[durationIndex]

#     if (len(durationDelays) < 5):
#         print(durationIndex, durationIndex * 30, durationCount, durationDelayedCount, '-')
#         meanDelayByDuration.append(0)
#         continue

#     # plotHistogram(durationDelays, binMin=0, binMax=600, binSize=2, ylim=None, color=None, histtype='bar', alpha=0.5)
#     # plotLineHistogram(durationDelays, binMin=0, binMax=600, binSize=30, ylim=None, color=None)

#     delayedFraction = durationDelayedCount / durationCount
#     delayedFractionByDuration.append(delayedFraction)
    
#     mu, std = stats.norm.fit(durationDelays)
#     meanDelayByDuration.append(mu)
#     stdDelayByDuration.append(std)
#     print(durationIndex, durationIndex * 30, durationCount, durationDelayedCount, delayedFraction)


# plt.plot(averageDelayByDuration)
# plt.xlim([0, 18])

# x = range(0, 18)

# y = meanDelayByDuration[0:18]
# coef = np.polyfit(x, y, 2)
# func = lambda x: coef[0] * x * x + coef[1] * x + coef[2]
# print('%sx^2 + %sx + %s' % (coef[0], coef[1], coef[2]))
# plt.plot(x, y, 'yo', x, func(x), '--k')

# y2 = stdDelayByDuration[0:18]
# coef2 = np.polyfit(x, y2, 1)
# func2 = lambda x: coef2[0] * x + coef2[1]
# print('%sx + %s' % (coef2[0], coef2[1]))
# plt.plot(x, y2, 'yo', x, func2(x), '--k')

# y3 = delayedFractionByDuration[0:18]
# coef3 = np.polyfit(x, y3, 1)
# func3 = lambda x: coef3[0] * x + coef3[1]
# print('%sx + %s' % (coef3[0], coef3[1]))
# plt.plot(x, y3, 'yo', x, func3(x), '--k')

# allPositiveDelays = [delay for delay in allPositiveDelays if delay < 600]

# a, loc, scale = stats.gamma.fit(allPositiveDelays)
# print(a, loc, scale)
# plotDistribution(lambda x: stats.gamma.cdf(x, a, loc, scale), binMin=0, binMax=600, binSize=2, ylim=[0,1])

# muAll, stdAll = stats.norm.fit(allPositiveDelays)
# print(muAll, stdAll)


# plotHistogram(allPositiveDelays, binMin=0, binMax=300, binSize=2, ylim=None)
# # plotDistribution(lambda x: stats.gamma.cdf(x, a, loc, scale), binMin=0, binMax=300, binSize=2, ylim=None, color=(0, 1, 0, 1))
# # plotDistribution(lambda x: stats.gamma.cdf(x, 1.69 * a, loc, 1.3 * scale), binMin=0, binMax=300, binSize=2, ylim=None, color='red')
# plotDistribution(lambda x: stats.gamma.cdf(x, 0.5, 0, 120), binMin=0, binMax=300, binSize=2, ylim=None, color='red')
# plotDistribution(lambda x: stats.gamma.cdf(x, 0.5, 0, 72), binMin=0, binMax=300, binSize=2, ylim=None, color=(0, 1, 0, 1))
# plotDistribution(lambda x: stats.gamma.cdf(x, 0.5, 0, 72), binMin=0, binMax=300, binSize=2, ylim=None, color=(0, 1, 0, 1))
# plt.ylim([0, 0.2])


# stats.probplot(allPositiveDelays, dist=stats.gamma, sparams=(a, loc, scale), plot=plt)
# stats.probplot(allPositiveDelays, dist=stats.gamma, sparams=(0.5, 0, 120), plot=plt)

# plotFittedNormalDistribution(mu=0, std=120, binMin=-120, binMax=120, binSize=8)

plt.show()

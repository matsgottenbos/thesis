import json
import matplotlib.pyplot as plt
import numpy as np
from scipy.stats import norm

def plotHistogram(values, binMin=-600, binMax=600, binSize=30):
    count = len(values)
    bins=np.arange(binMin, binMax + binSize, binSize)
    weights = np.ones(count) / count
    # plt.hist(values, bins=bins, histtype=u'step', color=(0, 0, 1, 0.2))
    plt.hist(values, bins=bins, histtype=u'step', color=(0, 0, 1, 1))

def plotLineHistogram(values, binMin=-600, binMax=600, binSize=30):
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

    # plt.plot(uniqueValues, countFractions, color=(0, 0, 1, 0.2))
    plt.plot(uniqueValues, countFractions, color=(0, 0, 1, 1))
    plt.ylim([binMin, binMax])
    plt.ylim([0, 1])

def plotFittedNormalDistribution(mu, std, binMin=-600, binMax=600, binSize=30):
    binCount = int((binMax - binMin) / binSize)
    
    # print(mu, std)
    x = np.linspace(binMin - binSize / 2, binMax + binSize / 2, binCount + 2)
    p = norm.pdf(x, mu, std)
    c = norm.cdf(x, mu, std)

    groupedCdf = [c[i + 1] - c[i] for i in range(len(c) - 1)]
    
    x2 = np.linspace(binMin, binMax, binCount + 1)
    plt.plot(x2, groupedCdf, color=(0, 0, 0, 0.2))
    plt.ylim([0, 1])


with open('./output/delays.json', 'r') as readFile:
    data = json.load(readFile)

activities = data['activities']

fig = plt.figure(figsize=(9,9))
ax = plt.subplot()

i = 0
allDelays = []
allRelativeDelays = []
muPlusStd = []
delayScores = []
delayInfos = []
for activity in activities:
    plannedDuration = max(15, activity['plannedDuration'])
    delays = activity['durationDelays']
    relativeDelays = [delay / plannedDuration for delay in delays]
    allDelays.extend(delays)
    allRelativeDelays.extend(relativeDelays)

    if activity['occurrenceCount'] < 10:
        continue

    hasNonZeroValue = False
    for delay in delays:
        if delay != 0: hasNonZeroValue = True

    if not hasNonZeroValue:
        continue

    mu, std = norm.fit(delays)
    muPlusStd.append(mu + std)

    count = len(delays)
    noDelayCount = 0
    subHourDelayCount = 0
    superHourDelayCount = 0
    for delay in delays:
        relativeDelay = delay / plannedDuration
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

    # plt.show()

    i += 1

# print(i)

# plotHistogram(allDelays)

# muPlusStd.sort()
# print(muPlusStd)
# plotHistogram(muPlusStd)

# delayScores.sort()
# print(delayScores)
# plotHistogram(delayScores, binMin=0, binMax=150, binSize=1)

# delayInfos.sort(key=lambda x: x[0])
# for delayInfo in delayInfos: print(delayInfo)

# plt.show()

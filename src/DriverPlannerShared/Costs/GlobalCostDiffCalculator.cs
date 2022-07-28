/*
 * Calculates cost differences for changes that are about multiple drivers together
*/

using System;

namespace DriverPlannerShared {
    public static class GlobalCostDiffCalculator {
        public static (SaExternalDriverTypeInfo, SaExternalDriverTypeInfo) GetExternalDriversGlobalCostDiff(Driver driver1, Driver driver2, SaDriverInfo driver1InfoDiff, SaDriverInfo driver2InfoDiff, SaInfo info) {
            if (driver1 is ExternalDriver externalDriver1) {
                if (driver2 is ExternalDriver externalDriver2) {
                    // Both drivers are external
                    if (externalDriver1.ExternalDriverTypeIndex == externalDriver2.ExternalDriverTypeIndex) {
                        // External drivers are of same type, so just count one
                        SaDriverInfo driverInfoDiff = driver1InfoDiff + driver2InfoDiff;
                        SaExternalDriverTypeInfo externalDriverTypeInfo = info.ExternalDriverTypeInfos[externalDriver1.ExternalDriverTypeIndex];
                        SaExternalDriverTypeInfo externalDriverTypeCostDiff = GetExternalDriverTypeCostDiff(externalDriver1, externalDriverTypeInfo, driverInfoDiff, info);
                        return (externalDriverTypeCostDiff, null);
                    } else {
                        // External drivers are of different types
                        SaExternalDriverTypeInfo externalDriverType1Info = info.ExternalDriverTypeInfos[externalDriver1.ExternalDriverTypeIndex];
                        SaExternalDriverTypeInfo externalDriverType1CostDiff = GetExternalDriverTypeCostDiff(externalDriver1, externalDriverType1Info, driver1InfoDiff, info);

                        SaExternalDriverTypeInfo externalDriverType2Info = info.ExternalDriverTypeInfos[externalDriver2.ExternalDriverTypeIndex];
                        SaExternalDriverTypeInfo externalDriverType2CostDiff = GetExternalDriverTypeCostDiff(externalDriver2, externalDriverType2Info, driver2InfoDiff, info);

                        return (externalDriverType1CostDiff, externalDriverType2CostDiff);
                    }
                } else {
                    // Only driver 1 is external
                    SaExternalDriverTypeInfo externalDriverType1Info = info.ExternalDriverTypeInfos[externalDriver1.ExternalDriverTypeIndex];
                    SaExternalDriverTypeInfo externalDriverType1CostDiff = GetExternalDriverTypeCostDiff(externalDriver1, externalDriverType1Info, driver1InfoDiff, info);
                    return (externalDriverType1CostDiff, null);
                }
            } else if (driver2 is ExternalDriver externalDriver2) {
                // Only driver 2 is external
                SaExternalDriverTypeInfo externalDriverType2Info = info.ExternalDriverTypeInfos[externalDriver2.ExternalDriverTypeIndex];
                SaExternalDriverTypeInfo externalDriverType2CostDiff = GetExternalDriverTypeCostDiff(externalDriver2, externalDriverType2Info, driver2InfoDiff, info);
                return (null, externalDriverType2CostDiff);
            }

            // Neither driver is external
            return (null, null);
        }

        static SaExternalDriverTypeInfo GetExternalDriverTypeCostDiff(ExternalDriver externalDriver, SaExternalDriverTypeInfo oldExternalDriverTypeInfo, SaDriverInfo driverInfoDiff, SaInfo info) {
#if DEBUG
            if (DevConfig.DebugCheckOperations) {
                SaDebugger.GetCurrentOperation().StartPart(string.Format("Global cost diff for external driver {0}", externalDriver.GetId()), externalDriver);
            }
#endif

            ExternalDriverType externalDriverType = info.Instance.ExternalDriverTypes[externalDriver.ExternalDriverTypeIndex];

            int newShiftCount = oldExternalDriverTypeInfo.ExternalShiftCount + driverInfoDiff.ShiftCount;
            SaExternalDriverTypeInfo newExternalDriverTypeInfo = new SaExternalDriverTypeInfo() {
                ExternalShiftCount = newShiftCount,
            };
            newExternalDriverTypeInfo.AddPotentialShiftCountViolation(newShiftCount, externalDriverType);

            SaExternalDriverTypeInfo externalDriverTypeInfoDiff = newExternalDriverTypeInfo - oldExternalDriverTypeInfo;

#if DEBUG
            if (DevConfig.DebugCheckOperations) {
                CheckErrors(externalDriverTypeInfoDiff, driverInfoDiff, externalDriver, info);
            }
#endif

            return externalDriverTypeInfoDiff;
        }


        /* Debugging */

        static void CheckErrors(SaExternalDriverTypeInfo operationExternalDriverTypeInfoDiff, SaDriverInfo driverInfoDiff, ExternalDriver externalDriver, SaInfo info) {
            // Old operation info
            SaDebugger.GetCurrentOperationPart().SetStage(OperationPartStage.OldNormal);
            SaExternalDriverTypeInfo oldOperationExternalDriverTypeInfo = info.ExternalDriverTypeInfos[externalDriver.ExternalDriverTypeIndex];
            SaDebugger.GetCurrentStageInfo().SetExternalDriverTypeInfo(oldOperationExternalDriverTypeInfo);

            // New operation info
            SaDebugger.GetCurrentOperationPart().SetStage(OperationPartStage.NewNormal);
            SaExternalDriverTypeInfo newOperationExternalDriverTypeInfo = oldOperationExternalDriverTypeInfo + operationExternalDriverTypeInfoDiff;
            SaDebugger.GetCurrentStageInfo().SetExternalDriverTypeInfo(newOperationExternalDriverTypeInfo);

            // Old checked info
            SaDebugger.GetCurrentOperationPart().SetStage(OperationPartStage.OldChecked);
            SaExternalDriverTypeInfo oldCheckedExternalDriverTypeInfo = TotalCostCalculator.GetExternalDriverTypeInfo(info.DriverInfos, externalDriver.ExternalDriverTypeIndex, info);
            SaDebugger.GetCurrentStageInfo().SetExternalDriverTypeInfo(oldCheckedExternalDriverTypeInfo);

            // Get driver infos after
            SaDriverInfo[] newDriverInfos = info.DriverInfos.Copy();
            newDriverInfos[externalDriver.AllDriversIndex] += driverInfoDiff;

            // New checked info
            SaDebugger.GetCurrentOperationPart().SetStage(OperationPartStage.NewChecked);
            SaExternalDriverTypeInfo newCheckedExternalDriverTypeInfo = TotalCostCalculator.GetExternalDriverTypeInfo(newDriverInfos, externalDriver.ExternalDriverTypeIndex, info);
            SaDebugger.GetCurrentStageInfo().SetExternalDriverTypeInfo(newCheckedExternalDriverTypeInfo);

            // Check for errors
            SaDebugger.GetCurrentOperationPart().CheckExternalDriverTypeErrors();
        }
    }
}

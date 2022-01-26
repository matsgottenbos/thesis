using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    class CostDiffCalculator {
        /* Cost diffs when unassigning/assigning */

        /** Determine cost differences when unassigning a trip from a driver */
        public static (double, double, double, int) UnassignTripCostDiff(Trip oldTrip, Driver driver, Trip tripToIgnore, Driver[] assignment, int driverOldWorkedTime, Instance instance, float penaltyFactor, int debugIterationNum) {
            SaDebugger.CurrentOperation.StartPart(string.Format("Unassign trip {0} from driver {1}", oldTrip.Index, driver.Index));
            (double costDiffWithoutCt, double costWithoutPenaltyDiff, double penaltyBaseDiffWithoutCt, int workDayLengthDiff) = UnassignTripCostDiffWithoutContractTime(oldTrip, driver, tripToIgnore, assignment, instance, penaltyFactor, debugIterationNum, false);

            // Worked time penalty
            float contractTimePenaltyBaseDiff = CostHelper.GetContractTimePenaltyBaseDiff(driverOldWorkedTime, driverOldWorkedTime + workDayLengthDiff, driver);
            float contractTimePenaltyDiff = contractTimePenaltyBaseDiff * penaltyFactor;

            double costDiff = costDiffWithoutCt + contractTimePenaltyDiff;
            double penaltyBaseDiff = penaltyBaseDiffWithoutCt + contractTimePenaltyBaseDiff;

            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.CurrentOperation.CurrentPart.CostDiff = costDiff;
                SaDebugger.CurrentOperation.CurrentPart.CostWithoutPenaltyDiff = costWithoutPenaltyDiff;
                SaDebugger.CurrentOperation.CurrentPart.PenaltyBaseDiff = penaltyBaseDiff;
            }

            return (costDiff, costWithoutPenaltyDiff, penaltyBaseDiff, workDayLengthDiff);
        }

        /** Determine cost differences when assigning a trip to a driver */
        public static (double, double, double, int) AssignTripCostDiff(Trip newTrip, Driver driver, Trip tripToIgnore, Driver[] assignment, int driverOldWorkedTime, Instance instance, float penaltyFactor, int debugIterationNum) {
            SaDebugger.CurrentOperation.StartPart(string.Format("Assign trip {0} to driver {1}", newTrip.Index, driver.Index));
            (double costDiffWithoutCt, double costWithoutPenaltyDiffWithoutCt, double penaltyBaseDiffWithoutCt, int workDayLengthDiffWithoutCt) = UnassignTripCostDiffWithoutContractTime(newTrip, driver, tripToIgnore, assignment, instance, penaltyFactor, debugIterationNum, true);

            // Worked time penalty
            float contractTimePenaltyBaseDiff = CostHelper.GetContractTimePenaltyBaseDiff(driverOldWorkedTime, driverOldWorkedTime - workDayLengthDiffWithoutCt, driver);
            float contractTimePenaltyDiff = contractTimePenaltyBaseDiff * penaltyFactor;

            double costDiff = -costDiffWithoutCt + contractTimePenaltyDiff;
            double costWithoutPenaltyDiff = -costWithoutPenaltyDiffWithoutCt;
            double penaltyBaseDiff = -penaltyBaseDiffWithoutCt + contractTimePenaltyBaseDiff;
            int workDayLengthDiff = -workDayLengthDiffWithoutCt;

            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.CurrentOperation.CurrentPart.CostDiff = costDiff;
                SaDebugger.CurrentOperation.CurrentPart.CostWithoutPenaltyDiff = costWithoutPenaltyDiff;
                SaDebugger.CurrentOperation.CurrentPart.PenaltyBaseDiff = penaltyBaseDiff;
            }

            return (costDiff, costWithoutPenaltyDiff, penaltyBaseDiff, workDayLengthDiff);
        }

        /** Method to determine the differences when unassigning/assigning a trip, excluding the contract time penalties*/

        static (double, double, double, int) UnassignTripCostDiffWithoutContractTime(Trip oldTrip, Driver driver, Trip tripToIgnore, Driver[] assignment, Instance instance, float penaltyFactor, int debugIterationNum, bool debugIsAssign) {
            (Trip tripBeforeSameShift, Trip tripBeforePrevShift) = CostHelper.GetTripBeforeInSameOrPrevShift(oldTrip, driver, tripToIgnore, assignment, instance);
            (Trip tripAfterSameShift, Trip tripAfterNextShift) = CostHelper.GetTripAfterInSameOrNextShift(oldTrip, driver, tripToIgnore, assignment, instance);

            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.CurrentOperation.CurrentPart.TripBeforeSameShift = tripBeforeSameShift;
                SaDebugger.CurrentOperation.CurrentPart.TripAfterSameShift = tripAfterSameShift;
                SaDebugger.CurrentOperation.CurrentPart.TripBeforePrevShift = tripBeforePrevShift;
                SaDebugger.CurrentOperation.CurrentPart.TripAfterNextShift = tripAfterNextShift;
            }

            int workDayLengthDiff;
            float precedencePenaltyDiff, workDayLengthPenaltyDiff, restTimePenaltyDiff;
            if (tripBeforeSameShift == null) {
                if (tripAfterSameShift == null) {
                    // No trip before or after
                    (workDayLengthDiff, precedencePenaltyDiff, workDayLengthPenaltyDiff, restTimePenaltyDiff) = UnassignOnlyTripCostDiff(oldTrip, tripBeforePrevShift, tripAfterNextShift, driver, tripToIgnore, assignment, instance, debugIsAssign);
                } else {
                    // Trip after, but not before
                    (workDayLengthDiff, precedencePenaltyDiff, workDayLengthPenaltyDiff, restTimePenaltyDiff) = UnassignFirstTripCostDiff(oldTrip, tripAfterSameShift, tripBeforePrevShift, driver, tripToIgnore, assignment, instance, debugIsAssign);
                }
            } else {
                if (tripAfterSameShift == null) {
                    // Trip before, but not after
                    (workDayLengthDiff, precedencePenaltyDiff, workDayLengthPenaltyDiff, restTimePenaltyDiff) = UnassignLastTripCostDiff(oldTrip, tripBeforeSameShift, tripAfterNextShift, driver, tripToIgnore, assignment, instance, debugIsAssign);
                } else {
                    // Trips before and after
                    (workDayLengthDiff, precedencePenaltyDiff, workDayLengthPenaltyDiff, restTimePenaltyDiff) = UnassignMiddleTripCostDiff(oldTrip, tripBeforeSameShift, tripAfterSameShift, driver, tripToIgnore, assignment, instance, debugIsAssign);
                }
            }

            // Debug
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.CurrentOperation.CurrentPart.DriversWorkedTimeDiff[driver.Index] += debugIsAssign ? -workDayLengthDiff : workDayLengthDiff;
            }

            double costWithoutPenaltyDiff = workDayLengthDiff * Config.SalaryRate;
            double penaltyBaseDiff = precedencePenaltyDiff + workDayLengthPenaltyDiff + restTimePenaltyDiff;
            double costDiff = costWithoutPenaltyDiff + penaltyBaseDiff * penaltyFactor;

            return (costDiff, costWithoutPenaltyDiff, penaltyBaseDiff, workDayLengthDiff);
        }


        /* Unassign/assign cost diffs for trips in specific places in their shift */

        static (int, float, float, float) UnassignOnlyTripCostDiff(Trip oldTrip, Trip tripBeforePrevShift, Trip tripAfterNextShift, Driver driver, Trip tripToIgnore, Driver[] assignment, Instance instance, bool debugIsAssign) {
            int workDayLengthDiff;
            float precedencePenaltyDiff = 0;
            float workDayLengthPenaltyDiff, restTimePenaltyDiff;

            // No trips before or after
            int oldWorkDayLength = CostHelper.WorkDayLength(oldTrip, oldTrip, driver, instance);
            workDayLengthDiff = -oldWorkDayLength;
            workDayLengthPenaltyDiff = -CostHelper.GetWorkDayLengthPenaltyBase(oldWorkDayLength);

            // Debug
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.CurrentOperation.CurrentPart.WorkDayLength.Add(oldWorkDayLength, 0, driver, debugIsAssign);
            }

            // Resting time
            if (tripBeforePrevShift == null) {
                if (tripAfterNextShift == null) {
                    // No prev or next shift
                    restTimePenaltyDiff = 0;

                    if (Config.DebugCheckAndLogOperations) {
                        SaDebugger.CurrentOperation.CurrentPart.ShiftInfoStr = debugIsAssign ? $"New only shift added" : "Only shift removed";
                    }
                } else {
                    // Next shift, but no prev shift
                    restTimePenaltyDiff = -CostHelper.GetRestTimePenaltyBase(oldTrip, tripAfterNextShift, driver);

                    if (Config.DebugCheckAndLogOperations) {
                        SaDebugger.CurrentOperation.CurrentPart.ShiftInfoStr = debugIsAssign ? "New first shift added" : "First shift removed";
                        int oldRestTimeNext = CostHelper.RestTime(oldTrip, tripAfterNextShift, driver);
                        SaDebugger.CurrentOperation.CurrentPart.RestTime.AddOld(oldRestTimeNext, driver, debugIsAssign);
                    }
                }
            } else {
                if (tripAfterNextShift == null) {
                    // Prev shift, but no next shift
                    restTimePenaltyDiff = -CostHelper.GetRestTimePenaltyBase(tripBeforePrevShift, oldTrip, driver);

                    if (Config.DebugCheckAndLogOperations) {
                        SaDebugger.CurrentOperation.CurrentPart.ShiftInfoStr = debugIsAssign ? "New last shift added" : "Last shift removed";
                        int oldRestTimePrev = CostHelper.RestTime(tripBeforePrevShift, oldTrip, driver);
                        SaDebugger.CurrentOperation.CurrentPart.RestTime.AddOld(oldRestTimePrev, driver, debugIsAssign);
                    }
                } else {
                    // Prev and next shift
                    restTimePenaltyDiff = CostHelper.GetRestTimePenaltyBase(tripBeforePrevShift, tripAfterNextShift, driver) - CostHelper.GetRestTimePenaltyBase(tripBeforePrevShift, oldTrip, driver) - CostHelper.GetRestTimePenaltyBase(oldTrip, tripAfterNextShift, driver);

                    if (Config.DebugCheckAndLogOperations) {
                        SaDebugger.CurrentOperation.CurrentPart.ShiftInfoStr = debugIsAssign ? "New middle shift added" : "Middle shift removed";
                        int oldRestTimePrev = CostHelper.RestTime(tripBeforePrevShift, oldTrip, driver);
                        int oldRestTimeNext = CostHelper.RestTime(oldTrip, tripAfterNextShift, driver);
                        int newRestTime = CostHelper.RestTime(tripBeforePrevShift, tripAfterNextShift, driver);
                        SaDebugger.CurrentOperation.CurrentPart.RestTime.AddOld(oldRestTimePrev, driver, debugIsAssign);
                        SaDebugger.CurrentOperation.CurrentPart.RestTime.AddOld(oldRestTimeNext, driver, debugIsAssign);
                        SaDebugger.CurrentOperation.CurrentPart.RestTime.AddNew(newRestTime, driver, debugIsAssign);
                    }

                    // TODO: check if shifts merge by unassigning
                }
            }

            return (workDayLengthDiff, precedencePenaltyDiff, workDayLengthPenaltyDiff, restTimePenaltyDiff);
        }

        static (int, float, float, float) UnassignFirstTripCostDiff(Trip oldTrip, Trip tripAfterSameShift, Trip tripBeforePrevShift, Driver driver, Trip tripToIgnore, Driver[] assignment, Instance instance, bool debugIsAssign) {
            // Work day length
            Trip lastDayTrip = CostHelper.GetLastTripOfShift(tripAfterSameShift, driver, tripToIgnore, assignment, instance);
            int oldWorkDayLength = CostHelper.WorkDayLength(oldTrip, lastDayTrip, driver, instance);
            int newWorkDayLength = CostHelper.WorkDayLength(tripAfterSameShift, lastDayTrip, driver, instance);
            int workDayLengthDiff = newWorkDayLength - oldWorkDayLength;
            float workDayLengthPenaltyDiff = CostHelper.GetWorkDayLengthPenaltyBaseDiff(oldWorkDayLength, newWorkDayLength);

            // Precedence
            float precedencePenaltyDiff = -CostHelper.GetPrecedencePenaltyBaseDiff(oldTrip, tripAfterSameShift, instance);

            // Debug
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.CurrentOperation.CurrentPart.Precedence.AddOld((oldTrip, tripAfterSameShift), driver, debugIsAssign);
                SaDebugger.CurrentOperation.CurrentPart.WorkDayLength.Add(oldWorkDayLength, newWorkDayLength, driver, debugIsAssign);
            }

            // Resting time (only need to check with previous trip)
            float restTimePenaltyDiff;
            if (tripBeforePrevShift == null) {
                // No prev shift
                restTimePenaltyDiff = 0;

                if (Config.DebugCheckAndLogOperations) {
                    SaDebugger.CurrentOperation.CurrentPart.ShiftInfoStr = debugIsAssign ? "New trip added at start of shift, without shift before" : "First trip of shift removed, without shift before";
                }
            } else {
                // Prev shift
                restTimePenaltyDiff = CostHelper.GetRestTimePenaltyBase(tripBeforePrevShift, tripAfterSameShift, driver) - CostHelper.GetRestTimePenaltyBase(tripBeforePrevShift, oldTrip, driver);

                if (Config.DebugCheckAndLogOperations) {
                    SaDebugger.CurrentOperation.CurrentPart.ShiftInfoStr = debugIsAssign ? "New trip added at start of shift, with shift before" : "First trip of shift removed, with shift before";
                    int oldRestTimePrev = CostHelper.RestTime(tripBeforePrevShift, oldTrip, driver);
                    SaDebugger.CurrentOperation.CurrentPart.RestTime.AddOld(oldRestTimePrev, driver, debugIsAssign);
                    int newRestTimePrev = CostHelper.RestTime(tripBeforePrevShift, tripAfterSameShift, driver);
                    SaDebugger.CurrentOperation.CurrentPart.RestTime.AddNew(newRestTimePrev, driver, debugIsAssign);
                }

                // TODO: check if shifts merge by unassigning
            }

            return (workDayLengthDiff, precedencePenaltyDiff, workDayLengthPenaltyDiff, restTimePenaltyDiff);
        }

        static (int, float, float, float) UnassignLastTripCostDiff(Trip oldTrip, Trip tripBeforeSameShift, Trip tripAfterNextShift, Driver driver, Trip tripToIgnore, Driver[] assignment, Instance instance, bool debugIsAssign) {
            // Work day length
            Trip firstDayTrip = CostHelper.GetFirstTripOfShift(tripBeforeSameShift, driver, tripToIgnore, assignment, instance);
            int workDayStartTime = CostHelper.WorkDayStartTimeWithTwoWayTravel(firstDayTrip, driver);
            int oldWorkDayLength = CostHelper.WorkDayEndTimeWithoutTwoWayTravel(firstDayTrip, oldTrip, instance) - workDayStartTime;
            int newWorkDayLength = CostHelper.WorkDayEndTimeWithoutTwoWayTravel(firstDayTrip, tripBeforeSameShift, instance) - workDayStartTime;
            int workDayLengthDiff = newWorkDayLength - oldWorkDayLength;
            float workDayLengthPenaltyDiff = CostHelper.GetWorkDayLengthPenaltyBaseDiff(oldWorkDayLength, newWorkDayLength);

            // Precedence
            float precedencePenaltyDiff = -CostHelper.GetPrecedencePenaltyBaseDiff(tripBeforeSameShift, oldTrip, instance);

            // Debug
            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.CurrentOperation.CurrentPart.Precedence.AddOld((tripBeforeSameShift, oldTrip), driver, debugIsAssign);
                SaDebugger.CurrentOperation.CurrentPart.WorkDayLength.Add(oldWorkDayLength, newWorkDayLength, driver, debugIsAssign);
            }

            // Resting time (only need to check with next trip)
            float restTimePenaltyDiff;
            if (tripAfterNextShift == null) {
                // No next shift
                restTimePenaltyDiff = 0;

                if (Config.DebugCheckAndLogOperations) {
                    SaDebugger.CurrentOperation.CurrentPart.ShiftInfoStr = debugIsAssign ? "New trip added at end of shift, without shift after" : "Last trip of shift removed, without shift after";
                }
            } else {
                // Next shift
                restTimePenaltyDiff = CostHelper.GetRestTimePenaltyBase(tripBeforeSameShift, tripAfterNextShift, driver) - CostHelper.GetRestTimePenaltyBase(oldTrip, tripAfterNextShift, driver);

                if (Config.DebugCheckAndLogOperations) {
                    SaDebugger.CurrentOperation.CurrentPart.ShiftInfoStr = debugIsAssign ? "New trip added at end of shift, with shift after" : "Last trip of shift removed, with shift after";
                    int oldRestTimeNext = CostHelper.RestTime(oldTrip, tripAfterNextShift, driver);
                    SaDebugger.CurrentOperation.CurrentPart.RestTime.AddOld(oldRestTimeNext, driver, debugIsAssign);
                    int newRestTimeNext = CostHelper.RestTime(tripBeforeSameShift, tripAfterNextShift, driver);
                    SaDebugger.CurrentOperation.CurrentPart.RestTime.AddNew(newRestTimeNext, driver, debugIsAssign);
                }

                // TODO: check if shifts merge by unassigning
            }

            return (workDayLengthDiff, precedencePenaltyDiff, workDayLengthPenaltyDiff, restTimePenaltyDiff);
        }

        static (int, float, float, float) UnassignMiddleTripCostDiff(Trip oldTrip, Trip tripBeforeSameShift, Trip tripAfterSameShift, Driver driver, Trip tripToIgnore, Driver[] assignment, Instance instance, bool debugIsAssign) {
            int workDayLengthDiff;
            float workDayLengthPenaltyDiff, restTimePenaltyDiff;
            int waitingTime = CostHelper.WaitingTime(tripBeforeSameShift, tripAfterSameShift, instance);
            if (waitingTime < Config.ShiftWaitingTimeThreshold) {
                // Trips before and after either don't exist or remain in the same shift
                workDayLengthDiff = 0;
                workDayLengthPenaltyDiff = 0;
                restTimePenaltyDiff = 0;

                // Debug
                if (Config.DebugCheckAndLogOperations) {
                    SaDebugger.CurrentOperation.CurrentPart.WorkDayLength.Add(0, 0, driver, debugIsAssign);
                    SaDebugger.CurrentOperation.CurrentPart.ShiftInfoStr = debugIsAssign ? "New trip added in middle of shift" : "Middle trip of shift removed";
                }
            } else {
                // Trips before and after become two separate shifts
                Trip firstDayTrip = CostHelper.GetFirstTripOfShift(tripBeforeSameShift, driver, tripToIgnore, assignment, instance);
                Trip lastDayTrip = CostHelper.GetLastTripOfShift(tripAfterSameShift, driver, tripToIgnore, assignment, instance);
                int oldWorkDayLength = CostHelper.WorkDayLength(firstDayTrip, lastDayTrip, driver, instance);
                int newWorkDayLengthPrev = CostHelper.WorkDayLength(firstDayTrip, tripBeforeSameShift, driver, instance);
                int newWorkDayLengthNext = CostHelper.WorkDayLength(tripAfterSameShift, lastDayTrip, driver, instance);
                workDayLengthDiff = newWorkDayLengthPrev + newWorkDayLengthNext - oldWorkDayLength;
                workDayLengthPenaltyDiff = CostHelper.GetWorkDayLengthPenaltyBase(newWorkDayLengthPrev) + CostHelper.GetWorkDayLengthPenaltyBase(newWorkDayLengthNext) - CostHelper.GetWorkDayLengthPenaltyBase(oldWorkDayLength);

                // Debug
                if (Config.DebugCheckAndLogOperations) {
                    SaDebugger.CurrentOperation.CurrentPart.WorkDayLength.AddOld(oldWorkDayLength, driver, debugIsAssign);
                    SaDebugger.CurrentOperation.CurrentPart.WorkDayLength.AddNew(newWorkDayLengthPrev, driver, debugIsAssign);
                    SaDebugger.CurrentOperation.CurrentPart.WorkDayLength.AddNew(newWorkDayLengthNext, driver, debugIsAssign);
                }

                // Resting time
                restTimePenaltyDiff = CostHelper.GetRestTimePenaltyBase(tripBeforeSameShift, tripAfterSameShift, driver);

                if (Config.DebugCheckAndLogOperations) {
                    SaDebugger.CurrentOperation.CurrentPart.ShiftInfoStr = debugIsAssign ? "Shift is merged" : "Shift is split";
                    int newRestTime = CostHelper.RestTime(tripBeforeSameShift, tripAfterSameShift, driver);
                    SaDebugger.CurrentOperation.CurrentPart.RestTime.AddNew(newRestTime, driver, debugIsAssign);
                }
            }

            // Precedence
            float precedencePenaltyDiff = CostHelper.GetPrecedencePenaltyBaseDiff(tripBeforeSameShift, tripAfterSameShift, instance) - CostHelper.GetPrecedencePenaltyBaseDiff(tripBeforeSameShift, oldTrip, instance) - CostHelper.GetPrecedencePenaltyBaseDiff(oldTrip, tripAfterSameShift, instance);

            if (Config.DebugCheckAndLogOperations) {
                SaDebugger.CurrentOperation.CurrentPart.Precedence.AddOld((tripBeforeSameShift, oldTrip), driver, debugIsAssign);
                SaDebugger.CurrentOperation.CurrentPart.Precedence.AddOld((oldTrip, tripAfterSameShift), driver, debugIsAssign);
                SaDebugger.CurrentOperation.CurrentPart.Precedence.AddNew((tripBeforeSameShift, tripAfterSameShift), driver, debugIsAssign);
            }

            return (workDayLengthDiff, precedencePenaltyDiff, workDayLengthPenaltyDiff, restTimePenaltyDiff);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thesis {
    static class RangeCostTripProcessor {
        public static void ProcessDriverTrip(Trip searchTrip, ref Trip shiftFirstTrip, ref Trip parkingTrip, ref Trip prevTrip, ref Trip beforeHotelTrip, Func<Trip, bool> isHotelAfterTrip, ref double costWithoutPenalty, ref double penalty, ref int workedTime, ref int shiftCount, Driver driver, SaInfo info, Instance instance, bool debugIsNew) {
            if (instance.AreSameShift(prevTrip, searchTrip)) {
                /* Same shift */
                // Check precedence
                penalty += PenaltyHelper.GetPrecedencePenalty(prevTrip, searchTrip, info, debugIsNew);

                // Check for invalid hotel stay
                if (isHotelAfterTrip(prevTrip)) {
                    penalty += PenaltyHelper.GetHotelPenalty(prevTrip, info, debugIsNew);
                }
            } else {
                /* Start of new shift */
                ProcessDriverEndNonFinalShift(searchTrip, ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, ref costWithoutPenalty, ref penalty, ref workedTime, ref shiftCount, driver, info, instance, debugIsNew);
            }

            prevTrip = searchTrip;
        }

        public static void ProcessDriverEndRange(Trip tripAfterRange, ref Trip shiftFirstTrip, ref Trip parkingTrip, ref Trip prevTrip, ref Trip beforeHotelTrip, Func<Trip, bool> isHotelAfterTrip, ref double costWithoutPenalty, ref double penalty, ref int workedTime, ref int shiftCount, Driver driver, SaInfo info, Instance instance, bool debugIsNew) {
            // If the range is not empty, finish the last shift of the range
            if (shiftFirstTrip != null) {
                if (tripAfterRange == null) {
                    // This is the end of the driver path
                    ProcessDriverEndFinalShift(ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, ref costWithoutPenalty, ref penalty, ref workedTime, ref shiftCount, driver, info, instance, debugIsNew);
                } else {
                    // This is the end of the range, but not the driver path
                    ProcessDriverEndNonFinalShift(tripAfterRange, ref shiftFirstTrip, ref parkingTrip, ref prevTrip, ref beforeHotelTrip, isHotelAfterTrip, ref costWithoutPenalty, ref penalty, ref workedTime, ref shiftCount, driver, info, instance, debugIsNew);
                }
            }
        }

        public static void ProcessDriverEndNonFinalShift(Trip searchTrip, ref Trip shiftFirstTrip, ref Trip parkingTrip, ref Trip prevTrip, ref Trip beforeHotelTrip, Func<Trip, bool> isHotelAfterTrip, ref double costWithoutPenalty, ref double penalty, ref int workedTime, ref int shiftCount, Driver driver, SaInfo info, Instance instance, bool debugIsNew) {
            shiftCount++;

            // Get travel time before
            int travelTimeBefore;
            if (beforeHotelTrip == null) {
                // No hotel stay before
                travelTimeBefore = driver.HomeTravelTimeToStart(shiftFirstTrip);
            } else {
                // Hotel stay before
                travelTimeBefore = instance.HalfTravelTimeViaHotel(beforeHotelTrip, shiftFirstTrip);
            }

            // Get driving time
            int shiftLengthWithoutTravel = instance.DrivingTime(shiftFirstTrip, prevTrip);
            float drivingCost = driver.DrivingCost(shiftFirstTrip, prevTrip);
            workedTime += shiftLengthWithoutTravel;

            // Get travel time after and rest time
            int travelTimeAfter, restTime;
            if (isHotelAfterTrip(prevTrip)) {
                // Hotel stay after
                travelTimeAfter = instance.HalfTravelTimeViaHotel(prevTrip, searchTrip);
                restTime = instance.RestTimeViaHotel(prevTrip, searchTrip);
                costWithoutPenalty += Config.HotelCosts;

                // Check if the hotel stay isn't too long
                if (restTime > Config.HotelMaxRestTime) {
                    penalty += PenaltyHelper.GetHotelPenalty(prevTrip, info, debugIsNew);
                }

                beforeHotelTrip = prevTrip;
            } else {
                // No hotel stay after
                travelTimeAfter = instance.CarTravelTime(prevTrip, parkingTrip) + driver.HomeTravelTimeToStart(parkingTrip);
                restTime = instance.RestTimeWithTravelTime(prevTrip, searchTrip, travelTimeAfter + driver.HomeTravelTimeToStart(searchTrip));

                // Set new parking trip
                parkingTrip = searchTrip;
                beforeHotelTrip = null;
            }

            // Get shift length
            int travelTime = travelTimeBefore + travelTimeAfter;
            int shiftLengthWithTravel = shiftLengthWithoutTravel + travelTime;

            // Get shift cost
            float travelCost = driver.GetPaidTravelCost(travelTime);
            float shiftCost = drivingCost + travelCost;
            costWithoutPenalty += shiftCost;

            // Check shift length
            penalty += PenaltyHelper.GetShiftLengthPenalty(shiftLengthWithoutTravel, shiftLengthWithTravel, debugIsNew);

            // Check rest time
            penalty += PenaltyHelper.GetRestTimePenalty(restTime, debugIsNew);

            // Start new shift
            shiftFirstTrip = searchTrip;
        }

        public static void ProcessDriverEndFinalShift(ref Trip shiftFirstTrip, ref Trip parkingTrip, ref Trip prevTrip, ref Trip beforeHotelTrip, Func<Trip, bool> isHotelAfterTrip, ref double costWithoutPenalty, ref double penalty, ref int workedTime, ref int shiftCount, Driver driver, SaInfo info, Instance instance, bool debugIsNew) {
            shiftCount++;

            // Get travel time before
            int travelTimeBefore;
            if (beforeHotelTrip == null) {
                // No hotel stay before
                travelTimeBefore = driver.HomeTravelTimeToStart(shiftFirstTrip);
            } else {
                // Hotel stay before
                travelTimeBefore = instance.HalfTravelTimeViaHotel(beforeHotelTrip, shiftFirstTrip);
            }

            // Get driving time
            int shiftLengthWithoutTravel = instance.DrivingTime(shiftFirstTrip, prevTrip);
            float drivingCost = driver.DrivingCost(shiftFirstTrip, prevTrip);
            workedTime += shiftLengthWithoutTravel;

            // Get travel time after
            int travelTimeAfter = instance.CarTravelTime(prevTrip, parkingTrip) + driver.HomeTravelTimeToStart(parkingTrip);

            // Get shift length
            int travelTime = travelTimeBefore + travelTimeAfter;
            int shiftLengthWithTravel = shiftLengthWithoutTravel + travelTime;

            // Get shift cost
            float travelCost = driver.GetPaidTravelCost(travelTime);
            float shiftCost = drivingCost + travelCost;
            costWithoutPenalty += shiftCost;

            // Check shift length
            penalty += PenaltyHelper.GetShiftLengthPenalty(shiftLengthWithoutTravel, shiftLengthWithTravel, debugIsNew);

            // Check for invalid hotel stay
            if (isHotelAfterTrip(prevTrip)) {
                penalty += PenaltyHelper.GetHotelPenalty(prevTrip, info, debugIsNew);
            }
        }
    }
}
